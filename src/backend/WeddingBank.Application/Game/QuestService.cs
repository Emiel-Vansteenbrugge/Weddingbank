using WeddingBank.Domain.Game;

namespace WeddingBank.Application.Game;

public sealed class QuestService(IQuestStore quests, ICaseStore cases, TimeProvider clock)
{
    public async Task<IReadOnlyList<QuestSummary>> ListAsync(CancellationToken cancellationToken)
    {
        var files = await quests.ListAsync(cancellationToken);
        var caseNames = await CaseNamesAsync(cancellationToken);
        var assignments = await quests.AssignmentsForAsync(files.SelectMany(file => file.Tasks.Select(task => task.Id)).ToArray(), cancellationToken);
        return files
            .OrderBy(file => file.DisplayOrder)
            .ThenBy(file => file.Reference)
            .Select(file => SummaryOf(file, caseNames, assignments))
            .ToArray();
    }

    public async Task<OperationResult<QuestDetail>> ReadAsync(Guid id, CancellationToken cancellationToken)
    {
        var file = await quests.FindAsync(id, cancellationToken);
        if (file is null)
        {
            return OperationResult<QuestDetail>.NotFound("That quest could not be found.");
        }

        return OperationResult<QuestDetail>.Ok(await DetailOfAsync(file, cancellationToken));
    }

    public async Task<OperationResult<QuestDetail>> CreateAsync(QuestDraft draft, CancellationToken cancellationToken)
    {
        if (!TryQuest(draft, out var reference, out var title, out var description, out var instructions, out var caseId, out var audience, out var kind, out var errors))
        {
            return OperationResult<QuestDetail>.Invalid(errors);
        }

        if (!await quests.CaseExistsAsync(caseId, cancellationToken))
        {
            return OperationResult<QuestDetail>.Invalid(Field("caseId", "Choose a case that already exists."));
        }

        if (await quests.ReferenceInUseAsync(reference, null, cancellationToken))
        {
            return OperationResult<QuestDetail>.Conflict("That quest reference is already in use.");
        }

        var files = await quests.ListAsync(cancellationToken);
        var order = files.Count == 0 ? 0 : files.Max(file => file.DisplayOrder) + 1;
        var file = Quest.Open(Guid.CreateVersion7(), reference, title, description, instructions, caseId, audience, kind, order, clock.GetUtcNow());
        quests.Add(file);
        await quests.SaveAsync(cancellationToken);
        return OperationResult<QuestDetail>.Ok(await DetailOfAsync(file, cancellationToken));
    }

    public async Task<OperationResult<QuestDetail>> UpdateAsync(Guid id, QuestDraft draft, CancellationToken cancellationToken)
    {
        var file = await quests.FindAsync(id, cancellationToken);
        if (file is null)
        {
            return OperationResult<QuestDetail>.NotFound("That quest could not be found.");
        }

        if (!TryQuest(draft, out var reference, out var title, out var description, out var instructions, out var caseId, out var audience, out var kind, out var errors))
        {
            return OperationResult<QuestDetail>.Invalid(errors);
        }

        if (!await quests.CaseExistsAsync(caseId, cancellationToken))
        {
            return OperationResult<QuestDetail>.Invalid(Field("caseId", "Choose a case that already exists."));
        }

        if (await quests.ReferenceInUseAsync(reference, id, cancellationToken))
        {
            return OperationResult<QuestDetail>.Conflict("That quest reference is already in use.");
        }

        if (caseId != file.CaseId && file.Tasks.Any(task => task.Unlocks.Count > 0))
        {
            return OperationResult<QuestDetail>.Invalid(Field("caseId", "Remove the document unlocks before moving this quest to another case."));
        }

        file.Revise(reference, title, description, instructions, caseId, audience, kind, clock.GetUtcNow());
        await quests.SaveAsync(cancellationToken);
        return OperationResult<QuestDetail>.Ok(await DetailOfAsync(file, cancellationToken));
    }

    public async Task<OperationResult<QuestDetail>> SetStatusAsync(Guid id, string? status, CancellationToken cancellationToken)
    {
        var file = await quests.FindAsync(id, cancellationToken);
        if (file is null)
        {
            return OperationResult<QuestDetail>.NotFound("That quest could not be found.");
        }

        if (!CaseStatuses.TryParse(status, out var parsed))
        {
            return OperationResult<QuestDetail>.Invalid(Field("status", "Choose draft, published, or archived."));
        }

        file.SetStatus(parsed, clock.GetUtcNow());
        await quests.SaveAsync(cancellationToken);
        return OperationResult<QuestDetail>.Ok(await DetailOfAsync(file, cancellationToken));
    }

    public async Task<OperationResult<TaskDetail>> AddTaskAsync(Guid questId, TaskDraft draft, CancellationToken cancellationToken)
    {
        var file = await quests.FindAsync(questId, cancellationToken);
        if (file is null)
        {
            return OperationResult<TaskDetail>.NotFound("That quest could not be found.");
        }

        var prepared = await TryTaskAsync(file, draft, cancellationToken);
        if (prepared is null)
        {
            return OperationResult<TaskDetail>.Invalid(_taskErrors);
        }

        if (await quests.TaskReferenceInUseAsync(prepared.Reference, null, cancellationToken))
        {
            return OperationResult<TaskDetail>.Conflict("That task reference is already in use.");
        }

        var now = clock.GetUtcNow();
        var order = file.Tasks.Count == 0 ? 0 : file.Tasks.Max(task => task.DisplayOrder) + 1;
        var task = file.AddTask(Guid.CreateVersion7(), prepared.Reference, prepared.Title, prepared.Instructions, prepared.TaskType, prepared.Scope, prepared.ParticipantLogin, prepared.IgnoreCase, prepared.TrimWhitespace, prepared.SuccessMessage, order, now);
        task.ReplaceAnswers(prepared.Answers.Select(answer => (Guid.CreateVersion7(), answer)).ToArray());
        task.ReplaceUnlocks(prepared.Documents.Select(documentId => (Guid.CreateVersion7(), documentId)).ToArray());
        var assignment = prepared.Scope == ProgressScope.Shared
            ? Assignment.ForTheHouse(Guid.CreateVersion7(), task.Reference, now, task.Id)
            : Assignment.ForParticipant(Guid.CreateVersion7(), task.Reference, prepared.ParticipantLogin!, now, task.Id);
        quests.AddAssignment(assignment);
        await quests.SaveAsync(cancellationToken);
        return OperationResult<TaskDetail>.Ok(await TaskOfAsync(task, assignment, cancellationToken));
    }

    public async Task<OperationResult<TaskDetail>> UpdateTaskAsync(Guid questId, Guid taskId, TaskDraft draft, CancellationToken cancellationToken)
    {
        var file = await quests.FindAsync(questId, cancellationToken);
        if (file is null || file.Tasks.All(task => task.Id != taskId))
        {
            return OperationResult<TaskDetail>.NotFound("That task could not be found.");
        }

        var prepared = await TryTaskAsync(file, draft, cancellationToken);
        if (prepared is null)
        {
            return OperationResult<TaskDetail>.Invalid(_taskErrors);
        }

        if (await quests.TaskReferenceInUseAsync(prepared.Reference, taskId, cancellationToken))
        {
            return OperationResult<TaskDetail>.Conflict("That task reference is already in use.");
        }

        var existing = (await quests.AssignmentsForAsync([taskId], cancellationToken)).SingleOrDefault();
        if (existing is null)
        {
            return OperationResult<TaskDetail>.NotFound("That task could not be found.");
        }

        var scopeChanged = existing.Scope != prepared.Scope || existing.ParticipantLogin != prepared.ParticipantLogin;
        if (scopeChanged && await quests.CountSubmissionsAsync(existing.Id, cancellationToken) > 0)
        {
            return OperationResult<TaskDetail>.Invalid(Field("scope", "This task already has submissions, so its assignment cannot change."));
        }

        var now = clock.GetUtcNow();
        var task = file.ReviseTask(taskId, prepared.Reference, prepared.Title, prepared.Instructions, prepared.TaskType, prepared.Scope, prepared.ParticipantLogin, prepared.IgnoreCase, prepared.TrimWhitespace, prepared.SuccessMessage, now);
        task.ReplaceAnswers(prepared.Answers.Select(answer => (Guid.CreateVersion7(), answer)).ToArray());
        task.ReplaceUnlocks(prepared.Documents.Select(documentId => (Guid.CreateVersion7(), documentId)).ToArray());
        var assignment = existing;
        if (scopeChanged)
        {
            quests.RemoveAssignment(existing);
            assignment = prepared.Scope == ProgressScope.Shared
                ? Assignment.ForTheHouse(Guid.CreateVersion7(), task.Reference, now, task.Id)
                : Assignment.ForParticipant(Guid.CreateVersion7(), task.Reference, prepared.ParticipantLogin!, now, task.Id);
            if (existing.Completed)
            {
                assignment.Complete(now);
            }

            quests.AddAssignment(assignment);
        }
        else if (!string.Equals(existing.Code, task.Reference, StringComparison.Ordinal))
        {
            existing.Rename(task.Reference, now);
        }

        await quests.SaveAsync(cancellationToken);
        return OperationResult<TaskDetail>.Ok(await TaskOfAsync(task, assignment, cancellationToken));
    }

    public async Task<OperationResult<TaskDetail>> SetTaskStatusAsync(Guid questId, Guid taskId, string? status, CancellationToken cancellationToken)
    {
        var file = await quests.FindAsync(questId, cancellationToken);
        if (file is null || file.Tasks.All(task => task.Id != taskId))
        {
            return OperationResult<TaskDetail>.NotFound("That task could not be found.");
        }

        if (!TaskStatuses.TryParse(status, out var parsed))
        {
            return OperationResult<TaskDetail>.Invalid(Field("status", "A task is either a draft or published."));
        }

        file.SetTaskStatus(taskId, parsed, clock.GetUtcNow());
        await quests.SaveAsync(cancellationToken);
        var assignment = (await quests.AssignmentsForAsync([taskId], cancellationToken)).Single();
        return OperationResult<TaskDetail>.Ok(await TaskOfAsync(file.FindTask(taskId), assignment, cancellationToken));
    }

    public async Task<IReadOnlyList<SubmissionRecord>> SubmissionsAsync(Guid questId, CancellationToken cancellationToken)
    {
        var file = await quests.FindAsync(questId, cancellationToken);
        if (file is null)
        {
            return [];
        }

        var assignments = await quests.AssignmentsForAsync(file.Tasks.Select(task => task.Id).ToArray(), cancellationToken);
        var submissions = await quests.SubmissionsForQuestAsync(questId, cancellationToken);
        return submissions.Select(submission =>
        {
            var assignment = assignments.Single(item => item.Id == submission.AssignmentId);
            var task = file.Tasks.Single(item => item.Id == assignment.TaskId);
            return new SubmissionRecord(submission.Id, task.Id, task.Reference, ProgressScopes.ToName(assignment.Scope), assignment.ParticipantLogin, submission.ParticipantLogin, submission.Answer, submission.Outcome, submission.SubmittedAt);
        }).ToArray();
    }

    public async Task<IReadOnlyList<ReadingQuestSummary>> ListForAsync(string login, CancellationToken cancellationToken)
    {
        var files = await quests.ListAsync(cancellationToken);
        var assignments = await quests.AssignmentsForAsync(TaskIds(files), cancellationToken);
        return files
            .Where(file => file.IsOpenTo(login) && file.IsReleased(assignments, login))
            .OrderBy(file => file.DisplayOrder)
            .ThenBy(file => file.Reference)
            .Select(file => ReadingSummaryOf(file, login, assignments))
            .OfType<ReadingQuestSummary>()
            .ToArray();
    }

    public async Task<OperationResult<ReadingQuest>> ReadForAsync(string login, Guid id, CancellationToken cancellationToken)
    {
        var file = await quests.FindAsync(id, cancellationToken);
        if (file is null || !file.IsOpenTo(login))
        {
            return OperationResult<ReadingQuest>.NotFound("That quest is not available.");
        }

        var assignments = await quests.AssignmentsForAsync(TaskIds([file]), cancellationToken);
        if (!file.IsReleased(assignments, login))
        {
            return OperationResult<ReadingQuest>.NotFound("That quest is not available.");
        }
        var reading = await WithCaseAsync(ReadingOf(file, login, assignments), cancellationToken);
        return reading.Tasks.Count == 0
            ? OperationResult<ReadingQuest>.NotFound("That quest is not available.")
            : OperationResult<ReadingQuest>.Ok(reading);
    }

    public async Task<OperationResult<AnswerOutcome>> SubmitAsync(string login, Guid taskId, string? answer, CancellationToken cancellationToken)
    {
        if (!Participants.IsParticipant(login))
        {
            return OperationResult<AnswerOutcome>.NotFound("That task is not available.");
        }

        var file = await quests.FindByTaskAsync(taskId, cancellationToken);
        if (file is null || !file.IsOpenTo(login))
        {
            return OperationResult<AnswerOutcome>.NotFound("That task is not available.");
        }

        var known = await quests.AssignmentsForAsync(TaskIds([file]), cancellationToken);
        if (!file.IsReleased(known, login))
        {
            return OperationResult<AnswerOutcome>.NotFound("That task is not available.");
        }

        var task = file.Tasks.Single(item => item.Id == taskId);
        var assignment = known.SingleOrDefault(item => item.TaskId == taskId);
        if (!task.IsVisibleTo(login, assignment) || assignment is null)
        {
            return OperationResult<AnswerOutcome>.NotFound("That task is not available.");
        }

        if (assignment.Completed)
        {
            return OperationResult<AnswerOutcome>.Conflict("This task is already complete.");
        }

        if (string.IsNullOrEmpty(answer) || answer.Length > 2000)
        {
            return OperationResult<AnswerOutcome>.Invalid(Field("answer", "Enter an answer."));
        }

        var now = clock.GetUtcNow();
        var accepted = task.Accepts(answer);
        quests.AddSubmission(Submission.Decide(Guid.CreateVersion7(), assignment.Id, login, answer, accepted, now));
        if (accepted)
        {
            assignment.Complete(now);
        }

        await quests.SaveAsync(cancellationToken);
        var message = accepted
            ? task.SuccessMessage ?? "The answer has been accepted."
            : QuestTask.Rejection;
        return OperationResult<AnswerOutcome>.Ok(new AnswerOutcome(accepted, assignment.Completed, message));
    }

    private Dictionary<string, string[]> _taskErrors = [];

    private async Task<PreparedTask?> TryTaskAsync(Quest file, TaskDraft draft, CancellationToken cancellationToken)
    {
        _taskErrors = [];
        var reference = Read("reference", () => CaseFile.ReferenceText(draft.Reference ?? ""), _taskErrors);
        var title = Read("title", () => CaseFile.Required(draft.Title ?? "", 160, "A task needs a title."), _taskErrors);
        var instructions = Read("instructions", () => CaseFile.Required(draft.Instructions ?? "", 4000, "A task needs instructions."), _taskErrors);
        if (!TaskTypes.TryParse(draft.TaskType, out var taskType))
        {
            _taskErrors["taskType"] = ["A task needs a known type."];
            taskType = "";
        }

        ProgressScope? scope = null;
        if (!ProgressScopes.TryParse(draft.Scope, out var parsedScope))
        {
            _taskErrors["scope"] = ["Choose an individual or shared assignment."];
        }
        else
        {
            scope = parsedScope;
        }

        var participant = ParticipantOf(scope, draft.Participant, _taskErrors);
        var ignoreCase = draft.IgnoreCase ?? false;
        var trimWhitespace = draft.TrimWhitespace ?? false;
        if (draft.IgnoreCase is null)
        {
            _taskErrors["ignoreCase"] = ["Choose whether letter case is ignored."];
        }

        if (draft.TrimWhitespace is null)
        {
            _taskErrors["trimWhitespace"] = ["Choose whether surrounding spaces are ignored."];
        }

        string? success = null;
        try
        {
            success = CaseFile.Optional(draft.SuccessMessage, 600);
        }
        catch (ArgumentException exception)
        {
            _taskErrors["successMessage"] = [exception.Message];
        }

        var answers = (draft.Answers ?? []).Select(answer => answer.Trim()).Where(answer => answer.Length > 0).Distinct(StringComparer.Ordinal).ToArray();
        if (answers.Length == 0 || answers.Any(answer => answer.Length > 500))
        {
            _taskErrors["answers"] = ["Enter the accepted answer."];
        }

        var documents = (draft.UnlockDocumentIds ?? []).Where(id => id != Guid.Empty).Distinct().ToArray();
        if (documents.Length > 0)
        {
            var found = await quests.DocumentsOnCaseAsync(file.CaseId, documents, cancellationToken);
            if (found.Count != documents.Length)
            {
                _taskErrors["unlockDocumentIds"] = ["Choose a document from this quest's case."];
            }
        }

        if (_taskErrors.Count > 0 || scope is null)
        {
            return null;
        }

        return new PreparedTask(reference, title, instructions, taskType, scope.Value, participant, ignoreCase, trimWhitespace, success, answers, documents);
    }

    private static bool TryQuest(
        QuestDraft draft,
        out string reference,
        out string title,
        out string description,
        out string instructions,
        out Guid caseId,
        out string audience,
        out string kind,
        out Dictionary<string, string[]> errors)
    {
        errors = [];
        reference = Read("reference", () => CaseFile.ReferenceText(draft.Reference ?? ""), errors);
        title = Read("title", () => CaseFile.Required(draft.Title ?? "", 160, "A quest needs a title."), errors);
        description = Read("description", () => CaseFile.Required(draft.Description ?? "", 2000, "A quest needs a description."), errors);
        instructions = Read("instructions", () => CaseFile.Required(draft.Instructions ?? "", 4000, "A quest needs instructions."), errors);
        caseId = draft.CaseId ?? Guid.Empty;
        if (caseId == Guid.Empty)
        {
            errors["caseId"] = ["Choose a case."];
        }

        audience = "";
        if (!Audiences.TryParse(draft.Audience, out audience))
        {
            errors["audience"] = ["Choose Juul, Amber, or both."];
        }

        kind = "";
        if (!QuestKinds.TryParse(draft.Kind, out kind))
        {
            errors["kind"] = ["Choose a main or side quest."];
        }

        return errors.Count == 0;
    }

    private static string? ParticipantOf(ProgressScope? scope, string? participant, Dictionary<string, string[]> errors)
    {
        if (scope is null)
        {
            return null;
        }

        if (scope == ProgressScope.Shared)
        {
            if (!string.IsNullOrWhiteSpace(participant))
            {
                errors["participant"] = ["A shared task is not assigned to one participant."];
            }

            return null;
        }

        var token = participant?.Trim().ToUpperInvariant();
        if (token is "JUUL" or "JUUL-V")
        {
            return Participants.Juul;
        }

        if (token is "AMBER" or "AMBER-D")
        {
            return Participants.Amber;
        }

        errors["participant"] = ["Choose Juul or Amber for an individual task."];
        return null;
    }

    private async Task<Dictionary<Guid, string>> CaseNamesAsync(CancellationToken cancellationToken)
    {
        var files = await cases.ListAsync(cancellationToken);
        return files.ToDictionary(file => file.Id, file => file.Reference);
    }

    private async Task<QuestDetail> DetailOfAsync(Quest file, CancellationToken cancellationToken)
    {
        var assignments = await quests.AssignmentsForAsync(file.Tasks.Select(task => task.Id).ToArray(), cancellationToken);
        var names = await CaseNamesAsync(cancellationToken);
        var tasks = new List<TaskDetail>();
        foreach (var task in file.Tasks.OrderBy(item => item.DisplayOrder).ThenBy(item => item.Reference))
        {
            var assignment = assignments.SingleOrDefault(item => item.TaskId == task.Id);
            tasks.Add(await TaskOfAsync(task, assignment, cancellationToken));
        }

        return new QuestDetail(file.Id, file.Reference, file.Title, file.Description, file.Instructions, file.CaseId, names.GetValueOrDefault(file.CaseId, ""), file.Status, file.Audience, file.Kind, file.DisplayOrder, file.CreatedAt, file.UpdatedAt, tasks);
    }

    private async Task<TaskDetail> TaskOfAsync(QuestTask task, Assignment? assignment, CancellationToken cancellationToken)
    {
        var described = await quests.DescribeDocumentsAsync(task.Unlocks.Select(unlock => unlock.DocumentId).ToArray(), cancellationToken);
        var released = assignment?.Completed == true;
        var unlocks = task.Unlocks.Select(unlock =>
        {
            var known = described.GetValueOrDefault(unlock.DocumentId);
            return new UnlockDetail(unlock.DocumentId, known.Reference ?? "", known.Title ?? "", released);
        }).ToArray();
        return new TaskDetail(task.Id, task.Reference, task.Title, task.Instructions, task.TaskType, task.Status, ProgressScopes.ToName(task.Scope), task.ParticipantLogin, task.IgnoreCase, task.TrimWhitespace, task.SuccessMessage, task.DisplayOrder, released, task.Answers.OrderBy(answer => answer.DisplayOrder).Select(answer => answer.Text).ToArray(), unlocks, task.UpdatedAt);
    }

    private static QuestSummary SummaryOf(Quest file, IReadOnlyDictionary<Guid, string> caseNames, IReadOnlyList<Assignment> assignments)
    {
        var ids = file.Tasks.Select(task => task.Id).ToHashSet();
        var mine = assignments.Where(item => item.TaskId is Guid taskId && ids.Contains(taskId)).ToArray();
        return new QuestSummary(file.Id, file.Reference, file.Title, file.Status, file.Audience, file.Kind, file.CaseId, caseNames.GetValueOrDefault(file.CaseId, ""), file.DisplayOrder, file.Tasks.Count, mine.Count(item => item.Completed), mine.Count(item => !item.Completed), file.UpdatedAt);
    }

    private static ReadingQuestSummary? ReadingSummaryOf(Quest file, string login, IReadOnlyList<Assignment> assignments)
    {
        var visible = VisibleTasks(file, login, assignments).ToArray();
        if (visible.Length == 0)
        {
            return null;
        }

        var completed = visible.Count(item => item.Assignment.Completed);
        return new ReadingQuestSummary(file.Id, file.Reference, file.Title, file.Description, visible.Length - completed, completed);
    }

    private static ReadingQuest ReadingOf(Quest file, string login, IReadOnlyList<Assignment> assignments)
    {
        var visible = VisibleTasks(file, login, assignments).OrderBy(item => item.Task.DisplayOrder).ThenBy(item => item.Task.Reference).ToArray();
        var shared = file.Tasks.Where(task => task.Status == TaskStatuses.Published && task.Scope == ProgressScope.Shared).Select(task => assignments.SingleOrDefault(item => item.TaskId == task.Id)).OfType<Assignment>().ToArray();
        var tasks = visible.Select(item => new ReadingTask(item.Task.Id, item.Task.Reference, item.Task.Title, item.Task.Instructions, item.Task.Scope == ProgressScope.Shared, item.Assignment.Completed, item.Assignment.Completed ? item.Task.SuccessMessage : null)).ToArray();
        return new ReadingQuest(file.Id, file.Reference, file.Title, file.Description, file.Instructions, file.CaseId, "", "", shared.Count(item => item.Completed), shared.Count(item => !item.Completed), tasks);
    }

    private async Task<ReadingQuest> WithCaseAsync(ReadingQuest reading, CancellationToken cancellationToken)
    {
        var names = await CaseNamesAsync(cancellationToken);
        var file = (await cases.ListAsync(cancellationToken)).SingleOrDefault(item => item.Id == reading.CaseId);
        return reading with { CaseReference = names.GetValueOrDefault(reading.CaseId, ""), CaseTitle = file?.Title ?? "" };
    }

    private static Guid[] TaskIds(IEnumerable<Quest> files)
    {
        return files
            .SelectMany(file => file.Tasks.Select(task => task.Id).Concat(file.ReleasedByTaskId is Guid release ? [release] : []))
            .Distinct()
            .ToArray();
    }

    private static IEnumerable<(QuestTask Task, Assignment Assignment)> VisibleTasks(Quest file, string login, IReadOnlyList<Assignment> assignments)
    {
        foreach (var task in file.Tasks)
        {
            var assignment = assignments.SingleOrDefault(item => item.TaskId == task.Id);
            if (task.IsVisibleTo(login, assignment) && assignment is not null)
            {
                yield return (task, assignment);
            }
        }
    }

    private static string Read(string key, Func<string> read, Dictionary<string, string[]> errors)
    {
        try
        {
            return read();
        }
        catch (ArgumentException exception)
        {
            errors[key] = [exception.Message];
            return "";
        }
    }

    private static Dictionary<string, string[]> Field(string key, string message)
    {
        return new Dictionary<string, string[]> { [key] = [message] };
    }

    private sealed record PreparedTask(
        string Reference,
        string Title,
        string Instructions,
        string TaskType,
        ProgressScope Scope,
        string? ParticipantLogin,
        bool IgnoreCase,
        bool TrimWhitespace,
        string? SuccessMessage,
        IReadOnlyList<string> Answers,
        IReadOnlyList<Guid> Documents);
}
