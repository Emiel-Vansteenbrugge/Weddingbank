using Microsoft.EntityFrameworkCore;
using WeddingBank.Domain.Game;
using WeddingBank.Infrastructure.Persistence;

namespace WeddingBank.Infrastructure.Game;

public sealed class ChapterOneReport
{
    public List<string> Lines { get; } = [];

    public List<string> Conflicts { get; } = [];
}

public sealed class ChapterOneInitializer(WeddingBankDbContext db, TimeProvider clock)
{
    public async Task<ChapterOneReport> EnsureAsync(CancellationToken cancellationToken = default)
    {
        var report = new ChapterOneReport();
        var now = clock.GetUtcNow();
        var references = ChapterOneCatalog.Documents.Select(document => document.Reference).ToArray();
        var file = await db.CaseFiles
            .Include(item => item.Documents)
            .SingleOrDefaultAsync(item => item.Reference == ChapterOneCatalog.CaseReference, cancellationToken);
        var misplacedQuery = db.CaseDocuments.Where(document => references.Contains(document.Reference));
        if (file is not null)
        {
            var caseId = file.Id;
            misplacedQuery = misplacedQuery.Where(document => document.CaseId != caseId);
        }

        var misplaced = await misplacedQuery.Select(document => document.Reference).ToListAsync(cancellationToken);
        if (misplaced.Count > 0)
        {
            report.Conflicts.Add($"{string.Join(", ", misplaced.Distinct().Order())} already belongs to another case.");
            return report;
        }

        if (file is null)
        {
            file = CaseFile.Open(Guid.CreateVersion7(), ChapterOneCatalog.CaseReference, ChapterOneCatalog.CaseTitle, ChapterOneCatalog.CaseSummary, ChapterOneCatalog.CaseIntroduction, Audiences.Both, 0, now);
            db.CaseFiles.Add(file);
            report.Lines.Add($"created case {ChapterOneCatalog.CaseReference}");
        }
        else if (ChangedCase(file))
        {
            file.Revise(ChapterOneCatalog.CaseReference, ChapterOneCatalog.CaseTitle, ChapterOneCatalog.CaseSummary, ChapterOneCatalog.CaseIntroduction, Audiences.Both, now);
            report.Lines.Add($"repaired case {ChapterOneCatalog.CaseReference}");
        }
        else
        {
            report.Lines.Add($"unchanged case {ChapterOneCatalog.CaseReference}");
        }

        if (file.Status != CaseStatuses.Published)
        {
            file.SetStatus(CaseStatuses.Published, now);
            report.Lines.Add($"published case {ChapterOneCatalog.CaseReference}");
        }

        for (var index = 0; index < ChapterOneCatalog.Documents.Length; index++)
        {
            EnsureDocument(file, ChapterOneCatalog.Documents[index], index, now, report);
        }

        var quest = await db.Quests
            .Include(item => item.Tasks)
            .ThenInclude(task => task.Answers)
            .Include(item => item.Tasks)
            .ThenInclude(task => task.Unlocks)
            .SingleOrDefaultAsync(item => item.Reference == ChapterOneCatalog.QuestReference, cancellationToken);
        if (quest is not null && quest.CaseId != file.Id)
        {
            return Conflict(report, $"{ChapterOneCatalog.QuestReference} is attached to a different case.");
        }

        var taskQuery = db.QuestTasks.Where(item => item.Reference == ChapterOneCatalog.TaskReference);
        if (quest is not null)
        {
            var questId = quest.Id;
            taskQuery = taskQuery.Where(item => item.QuestId != questId);
        }

        if (await taskQuery.AnyAsync(cancellationToken))
        {
            return Conflict(report, $"{ChapterOneCatalog.TaskReference} already belongs to another quest.");
        }

        if (quest is null)
        {
            var order = await db.Quests.Select(item => (int?)item.DisplayOrder).MaxAsync(cancellationToken) ?? -1;
            quest = Quest.Open(Guid.CreateVersion7(), ChapterOneCatalog.QuestReference, ChapterOneCatalog.QuestTitle, ChapterOneCatalog.QuestDescription, ChapterOneCatalog.QuestInstructions, file.Id, Audiences.Both, QuestKinds.Main, order + 1, now);
            db.Quests.Add(quest);
            report.Lines.Add($"created quest {ChapterOneCatalog.QuestReference}");
        }
        else if (ChangedQuest(quest))
        {
            quest.Revise(ChapterOneCatalog.QuestReference, ChapterOneCatalog.QuestTitle, ChapterOneCatalog.QuestDescription, ChapterOneCatalog.QuestInstructions, file.Id, Audiences.Both, QuestKinds.Main, now);
            report.Lines.Add($"repaired quest {ChapterOneCatalog.QuestReference}");
        }
        else
        {
            report.Lines.Add($"unchanged quest {ChapterOneCatalog.QuestReference}");
        }

        if (quest.Status != CaseStatuses.Published)
        {
            quest.SetStatus(CaseStatuses.Published, now);
            report.Lines.Add($"published quest {ChapterOneCatalog.QuestReference}");
        }

        var task = quest.Tasks.SingleOrDefault(item => item.Reference == ChapterOneCatalog.TaskReference);
        if (task is null)
        {
            var taskOrder = quest.Tasks.Count == 0 ? 0 : quest.Tasks.Max(item => item.DisplayOrder) + 1;
            task = quest.AddTask(Guid.CreateVersion7(), ChapterOneCatalog.TaskReference, ChapterOneCatalog.TaskTitle, ChapterOneCatalog.TaskInstructions, TaskTypes.Text, ProgressScope.Shared, null, true, true, ChapterOneCatalog.SuccessMessage, taskOrder, now);
            report.Lines.Add($"created task {ChapterOneCatalog.TaskReference}");
        }
        else if (await RepairTaskAsync(quest, task, now, report, cancellationToken) is false)
        {
            return Conflict(report, null);
        }

        if (task.Status != TaskStatuses.Published)
        {
            quest.SetTaskStatus(task.Id, TaskStatuses.Published, now);
            report.Lines.Add($"published task {ChapterOneCatalog.TaskReference}");
        }

        var locked = file.Documents.Single(document => document.Reference == ChapterOneCatalog.LockedDocument);
        EnsureAnswer(task, report);
        EnsureUnlock(task, locked.Id, report);
        if (await EnsureAssignmentAsync(task, now, report, cancellationToken) is false)
        {
            return Conflict(report, null);
        }

        await db.SaveChangesAsync(cancellationToken);
        return report;
    }

    private static void EnsureDocument(CaseFile file, ChapterOneDocument spec, int order, DateTimeOffset now, ChapterOneReport report)
    {
        var document = file.Documents.SingleOrDefault(item => item.Reference == spec.Reference);
        var wanted = spec.Published ? DocumentStatuses.Published : DocumentStatuses.Draft;
        if (document is null)
        {
            document = file.AddDocument(Guid.CreateVersion7(), spec.Reference, spec.Title, spec.Category, null, spec.Content, spec.Audience, order, now);
            report.Lines.Add($"created document {spec.Reference}");
        }
        else if (ChangedDocument(document, spec))
        {
            file.ReviseDocument(document.Id, spec.Reference, spec.Title, spec.Category, document.Description, spec.Content, spec.Audience, now);
            report.Lines.Add($"repaired document {spec.Reference}");
        }
        else
        {
            report.Lines.Add($"unchanged document {spec.Reference}");
        }

        if (document.Status != wanted)
        {
            file.SetDocumentStatus(document.Id, wanted, now);
            report.Lines.Add($"{(spec.Published ? "published" : "held")} document {spec.Reference}");
        }
    }

    private async Task<bool> RepairTaskAsync(Quest quest, QuestTask task, DateTimeOffset now, ChapterOneReport report, CancellationToken cancellationToken)
    {
        var assignment = await db.Assignments.SingleOrDefaultAsync(item => item.TaskId == task.Id, cancellationToken);
        var scopeChanged = task.Scope != ProgressScope.Shared || task.ParticipantLogin is not null;
        if (scopeChanged && assignment is not null && (assignment.Completed || await db.Submissions.AnyAsync(item => item.AssignmentId == assignment.Id, cancellationToken)))
        {
            report.Conflicts.Add($"{ChapterOneCatalog.TaskReference} already has progress, so its shared assignment cannot be repaired.");
            return false;
        }

        if (ChangedTask(task) || scopeChanged)
        {
            quest.ReviseTask(task.Id, ChapterOneCatalog.TaskReference, ChapterOneCatalog.TaskTitle, ChapterOneCatalog.TaskInstructions, TaskTypes.Text, ProgressScope.Shared, null, true, true, ChapterOneCatalog.SuccessMessage, now);
            report.Lines.Add($"repaired task {ChapterOneCatalog.TaskReference}");
        }
        else
        {
            report.Lines.Add($"unchanged task {ChapterOneCatalog.TaskReference}");
        }

        return true;
    }

    private static void EnsureAnswer(QuestTask task, ChapterOneReport report)
    {
        var answers = task.Answers.OrderBy(answer => answer.DisplayOrder).Select(answer => answer.Text).ToArray();
        if (answers.Length == 1 && answers[0] == ChapterOneCatalog.AcceptedAnswer)
        {
            report.Lines.Add($"unchanged answer {ChapterOneCatalog.TaskReference}");
            return;
        }

        task.ReplaceAnswers([(Guid.CreateVersion7(), ChapterOneCatalog.AcceptedAnswer)]);
        report.Lines.Add($"{(answers.Length == 0 ? "recorded" : "repaired")} answer {ChapterOneCatalog.TaskReference}");
    }

    private static void EnsureUnlock(QuestTask task, Guid documentId, ChapterOneReport report)
    {
        if (task.Unlocks.Any(unlock => unlock.DocumentId == documentId))
        {
            report.Lines.Add($"unchanged unlock {ChapterOneCatalog.LockedDocument}");
            return;
        }

        var unlocks = task.Unlocks.Select(unlock => (unlock.Id, unlock.DocumentId)).Append((Guid.CreateVersion7(), documentId)).ToArray();
        task.ReplaceUnlocks(unlocks);
        report.Lines.Add($"linked unlock {ChapterOneCatalog.LockedDocument}");
    }

    private async Task<bool> EnsureAssignmentAsync(QuestTask task, DateTimeOffset now, ChapterOneReport report, CancellationToken cancellationToken)
    {
        var assignment = await db.Assignments.SingleOrDefaultAsync(item => item.TaskId == task.Id, cancellationToken);
        if (assignment is null)
        {
            db.Assignments.Add(Assignment.ForTheHouse(Guid.CreateVersion7(), task.Reference, now, task.Id));
            report.Lines.Add($"created shared assignment {ChapterOneCatalog.TaskReference}");
            return true;
        }

        if (assignment.Scope != ProgressScope.Shared || assignment.ParticipantLogin is not null)
        {
            if (assignment.Completed || await db.Submissions.AnyAsync(item => item.AssignmentId == assignment.Id, cancellationToken))
            {
                report.Conflicts.Add($"{ChapterOneCatalog.TaskReference} already has progress, so its shared assignment cannot be repaired.");
                return false;
            }

            db.Assignments.Remove(assignment);
            db.Assignments.Add(Assignment.ForTheHouse(Guid.CreateVersion7(), task.Reference, now, task.Id));
            report.Lines.Add($"repaired shared assignment {ChapterOneCatalog.TaskReference}");
            return true;
        }

        if (!string.Equals(assignment.Code, task.Reference, StringComparison.Ordinal))
        {
            assignment.Rename(task.Reference, now);
            report.Lines.Add($"repaired shared assignment {ChapterOneCatalog.TaskReference}");
        }
        else
        {
            report.Lines.Add(assignment.Completed
                ? $"kept completed assignment {ChapterOneCatalog.TaskReference}"
                : $"unchanged shared assignment {ChapterOneCatalog.TaskReference}");
        }

        return true;
    }

    private static bool ChangedCase(CaseFile file)
    {
        return file.Title != ChapterOneCatalog.CaseTitle
            || file.Summary != ChapterOneCatalog.CaseSummary
            || (file.Introduction ?? "") != ChapterOneCatalog.CaseIntroduction
            || file.Audience != Audiences.Both;
    }

    private static bool ChangedQuest(Quest quest)
    {
        return quest.Title != ChapterOneCatalog.QuestTitle
            || quest.Description != ChapterOneCatalog.QuestDescription
            || quest.Instructions != ChapterOneCatalog.QuestInstructions
            || quest.Audience != Audiences.Both
            || quest.Kind != QuestKinds.Main;
    }

    private static bool ChangedTask(QuestTask task)
    {
        return task.Title != ChapterOneCatalog.TaskTitle
            || task.Instructions != ChapterOneCatalog.TaskInstructions
            || task.TaskType != TaskTypes.Text
            || !task.IgnoreCase
            || !task.TrimWhitespace
            || task.SuccessMessage != ChapterOneCatalog.SuccessMessage;
    }

    private ChapterOneReport Conflict(ChapterOneReport report, string? conflict)
    {
        if (conflict is not null)
        {
            report.Conflicts.Add(conflict);
        }

        report.Lines.Clear();
        db.ChangeTracker.Clear();
        return report;
    }

    private static bool ChangedDocument(CaseDocument document, ChapterOneDocument spec)
    {
        return document.Title != spec.Title
            || document.Category != spec.Category
            || document.Content != spec.Content
            || document.Audience != spec.Audience;
    }
}
