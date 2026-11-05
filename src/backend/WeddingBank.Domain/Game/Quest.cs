namespace WeddingBank.Domain.Game;

public static class QuestKinds
{
    public const string Main = "MAIN";
    public const string Side = "SIDE";

    public static bool TryParse(string? value, out string kind)
    {
        kind = value?.Trim().ToUpperInvariant() ?? "";
        if (kind is Main or Side)
        {
            return true;
        }

        kind = "";
        return false;
    }
}

public static class TaskTypes
{
    public const string Text = "TEXT";

    public static bool TryParse(string? value, out string taskType)
    {
        taskType = value?.Trim().ToUpperInvariant() ?? "";
        if (taskType is Text)
        {
            return true;
        }

        taskType = "";
        return false;
    }
}

public static class TaskStatuses
{
    public const string Draft = "DRAFT";
    public const string Published = "PUBLISHED";

    public static bool TryParse(string? value, out string status)
    {
        status = value?.Trim().ToUpperInvariant() ?? "";
        if (status is Draft or Published)
        {
            return true;
        }

        status = "";
        return false;
    }
}

public sealed class Quest
{
    private readonly List<QuestTask> _tasks = [];

    private Quest()
    {
    }

    public Guid Id { get; private set; }

    public string Reference { get; private set; } = "";

    public string Title { get; private set; } = "";

    public string Description { get; private set; } = "";

    public string Instructions { get; private set; } = "";

    public Guid CaseId { get; private set; }

    public string Audience { get; private set; } = Audiences.Both;

    public string Kind { get; private set; } = QuestKinds.Main;

    public string Status { get; private set; } = CaseStatuses.Draft;

    public int DisplayOrder { get; private set; }

    public Guid? ReleasedByTaskId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<QuestTask> Tasks => _tasks;

    public static Quest Open(
        Guid id,
        string reference,
        string title,
        string description,
        string instructions,
        Guid caseId,
        string audience,
        string kind,
        int displayOrder,
        DateTimeOffset now)
    {
        return new Quest
        {
            Id = id,
            Reference = CaseFile.ReferenceText(reference),
            Title = CaseFile.Required(title, 160, "A quest needs a title."),
            Description = CaseFile.Required(description, 2000, "A quest needs a description."),
            Instructions = CaseFile.Required(instructions, 4000, "A quest needs instructions."),
            CaseId = caseId,
            Audience = KnownAudience(audience),
            Kind = KnownKind(kind),
            Status = CaseStatuses.Draft,
            DisplayOrder = displayOrder,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void Revise(
        string reference,
        string title,
        string description,
        string instructions,
        Guid caseId,
        string audience,
        string kind,
        DateTimeOffset now)
    {
        Reference = CaseFile.ReferenceText(reference);
        Title = CaseFile.Required(title, 160, "A quest needs a title.");
        Description = CaseFile.Required(description, 2000, "A quest needs a description.");
        Instructions = CaseFile.Required(instructions, 4000, "A quest needs instructions.");
        CaseId = caseId;
        Audience = KnownAudience(audience);
        Kind = KnownKind(kind);
        UpdatedAt = now;
    }

    public void SetStatus(string status, DateTimeOffset now)
    {
        Status = CaseStatuses.TryParse(status, out var parsed)
            ? parsed
            : throw new ArgumentException("A quest status must be draft, published, or archived.", nameof(status));
        UpdatedAt = now;
    }

    public void Place(int displayOrder, DateTimeOffset now)
    {
        DisplayOrder = displayOrder;
        UpdatedAt = now;
    }

    public void HoldUntil(Guid taskId, DateTimeOffset now)
    {
        ReleasedByTaskId = taskId;
        UpdatedAt = now;
    }

    public bool IsOpenTo(string login)
    {
        return Status == CaseStatuses.Published && Audiences.Includes(Audience, login);
    }

    public bool IsReleased(IEnumerable<Assignment> assignments, string login)
    {
        return ReleasedByTaskId is not Guid taskId
            || assignments.Any(assignment => assignment.TaskId == taskId && assignment.CountsFor(login) && assignment.Completed);
    }

    public QuestTask AddTask(
        Guid id,
        string reference,
        string title,
        string instructions,
        string taskType,
        ProgressScope scope,
        string? participantLogin,
        bool ignoreCase,
        bool trimWhitespace,
        string? successMessage,
        int displayOrder,
        DateTimeOffset now)
    {
        var taskReference = CaseFile.ReferenceText(reference);
        if (_tasks.Any(task => task.Reference == taskReference))
        {
            throw new InvalidOperationException("That task reference is already used in this quest.");
        }

        var task = QuestTask.Create(id, Id, taskReference, title, instructions, taskType, scope, participantLogin, ignoreCase, trimWhitespace, successMessage, displayOrder, now);
        _tasks.Add(task);
        UpdatedAt = now;
        return task;
    }

    public QuestTask ReviseTask(
        Guid taskId,
        string reference,
        string title,
        string instructions,
        string taskType,
        ProgressScope scope,
        string? participantLogin,
        bool ignoreCase,
        bool trimWhitespace,
        string? successMessage,
        DateTimeOffset now)
    {
        var task = FindTask(taskId);
        var taskReference = CaseFile.ReferenceText(reference);
        if (_tasks.Any(item => item.Id != taskId && item.Reference == taskReference))
        {
            throw new InvalidOperationException("That task reference is already used in this quest.");
        }

        task.Revise(taskReference, title, instructions, taskType, scope, participantLogin, ignoreCase, trimWhitespace, successMessage, now);
        UpdatedAt = now;
        return task;
    }

    public void SetTaskStatus(Guid taskId, string status, DateTimeOffset now)
    {
        FindTask(taskId).SetStatus(status, now);
        UpdatedAt = now;
    }

    public void OrderTasks(IReadOnlyList<Guid> ids, DateTimeOffset now)
    {
        if (ids.Count != _tasks.Count || ids.Distinct().Count() != ids.Count || ids.Any(id => _tasks.All(task => task.Id != id)))
        {
            throw new ArgumentException("The task order must list each task in this quest once.", nameof(ids));
        }

        for (var index = 0; index < ids.Count; index++)
        {
            FindTask(ids[index]).Place(index, now);
        }

        UpdatedAt = now;
    }

    public QuestTask FindTask(Guid taskId)
    {
        return _tasks.SingleOrDefault(task => task.Id == taskId)
            ?? throw new InvalidOperationException("That task is not in this quest.");
    }

    private static string KnownAudience(string audience)
    {
        return Audiences.TryParse(audience, out var parsed)
            ? parsed
            : throw new ArgumentException("A quest is for Juul, Amber, or both.", nameof(audience));
    }

    private static string KnownKind(string kind)
    {
        return QuestKinds.TryParse(kind, out var parsed)
            ? parsed
            : throw new ArgumentException("A quest is main or side.", nameof(kind));
    }
}

public sealed class QuestTask
{
    public const string Rejection = "The submitted reference could not be verified.";

    private readonly List<TaskAnswer> _answers = [];

    private readonly List<DocumentUnlock> _unlocks = [];

    private QuestTask()
    {
    }

    public Guid Id { get; private set; }

    public Guid QuestId { get; private set; }

    public string Reference { get; private set; } = "";

    public string Title { get; private set; } = "";

    public string Instructions { get; private set; } = "";

    public string TaskType { get; private set; } = TaskTypes.Text;

    public ProgressScope Scope { get; private set; }

    public string? ParticipantLogin { get; private set; }

    public bool IgnoreCase { get; private set; }

    public bool TrimWhitespace { get; private set; }

    public string? SuccessMessage { get; private set; }

    public int DisplayOrder { get; private set; }

    public string Status { get; private set; } = TaskStatuses.Draft;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<TaskAnswer> Answers => _answers;

    public IReadOnlyList<DocumentUnlock> Unlocks => _unlocks;

    internal static QuestTask Create(
        Guid id,
        Guid questId,
        string reference,
        string title,
        string instructions,
        string taskType,
        ProgressScope scope,
        string? participantLogin,
        bool ignoreCase,
        bool trimWhitespace,
        string? successMessage,
        int displayOrder,
        DateTimeOffset now)
    {
        var task = new QuestTask
        {
            Id = id,
            QuestId = questId,
            CreatedAt = now,
        };
        task.Revise(reference, title, instructions, taskType, scope, participantLogin, ignoreCase, trimWhitespace, successMessage, now);
        task.DisplayOrder = displayOrder;
        task.Status = TaskStatuses.Draft;
        return task;
    }

    internal void Revise(
        string reference,
        string title,
        string instructions,
        string taskType,
        ProgressScope scope,
        string? participantLogin,
        bool ignoreCase,
        bool trimWhitespace,
        string? successMessage,
        DateTimeOffset now)
    {
        if (!TaskTypes.TryParse(taskType, out var parsedType))
        {
            throw new ArgumentException("A task needs a known type.", nameof(taskType));
        }

        if (scope == ProgressScope.Shared && participantLogin is not null)
        {
            throw new ArgumentException("A shared task is not assigned to one participant.", nameof(participantLogin));
        }

        if (scope == ProgressScope.Individual && !Participants.IsParticipant(participantLogin))
        {
            throw new ArgumentException("An individual task belongs to Juul or Amber.", nameof(participantLogin));
        }

        Reference = reference;
        Title = CaseFile.Required(title, 160, "A task needs a title.");
        Instructions = CaseFile.Required(instructions, 4000, "A task needs instructions.");
        TaskType = parsedType;
        Scope = scope;
        ParticipantLogin = scope == ProgressScope.Shared ? null : participantLogin;
        IgnoreCase = ignoreCase;
        TrimWhitespace = trimWhitespace;
        SuccessMessage = CaseFile.Optional(successMessage, 600);
        UpdatedAt = now;
    }

    internal void SetStatus(string status, DateTimeOffset now)
    {
        Status = TaskStatuses.TryParse(status, out var parsed)
            ? parsed
            : throw new ArgumentException("A task is either a draft or published.", nameof(status));
        UpdatedAt = now;
    }

    internal void Place(int displayOrder, DateTimeOffset now)
    {
        DisplayOrder = displayOrder;
        UpdatedAt = now;
    }

    public void ReplaceAnswers(IReadOnlyList<(Guid Id, string Text)> answers)
    {
        if (answers.Count is < 1 or > 12)
        {
            throw new ArgumentException("A task needs one accepted answer, and no more than twelve.", nameof(answers));
        }

        _answers.Clear();
        for (var index = 0; index < answers.Count; index++)
        {
            _answers.Add(TaskAnswer.Create(answers[index].Id, Id, answers[index].Text, index));
        }
    }

    public void ReplaceUnlocks(IReadOnlyList<(Guid Id, Guid DocumentId)> unlocks)
    {
        if (unlocks.Count > 12 || unlocks.Select(item => item.DocumentId).Distinct().Count() != unlocks.Count)
        {
            throw new ArgumentException("Each document can be unlocked once by this task.", nameof(unlocks));
        }

        _unlocks.Clear();
        foreach (var unlock in unlocks)
        {
            _unlocks.Add(DocumentUnlock.Create(unlock.Id, Id, unlock.DocumentId));
        }
    }

    public bool Accepts(string submitted)
    {
        if (TaskType != TaskTypes.Text || string.IsNullOrEmpty(submitted))
        {
            return false;
        }

        var candidate = Normalize(submitted);
        if (candidate.Length == 0)
        {
            return false;
        }

        var comparison = IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return _answers.Any(answer => string.Equals(Normalize(answer.Text), candidate, comparison));
    }

    public bool IsVisibleTo(string login, Assignment? assignment)
    {
        return Status == TaskStatuses.Published
            && assignment is not null
            && assignment.TaskId == Id
            && assignment.CountsFor(login);
    }

    private string Normalize(string value)
    {
        return TrimWhitespace ? value.Trim() : value;
    }
}

public sealed class TaskAnswer
{
    private TaskAnswer()
    {
    }

    public Guid Id { get; private set; }

    public Guid TaskId { get; private set; }

    public string Text { get; private set; } = "";

    public int DisplayOrder { get; private set; }

    internal static TaskAnswer Create(Guid id, Guid taskId, string text, int displayOrder)
    {
        return new TaskAnswer
        {
            Id = id,
            TaskId = taskId,
            Text = CaseFile.Required(text, 500, "An accepted answer needs text."),
            DisplayOrder = displayOrder,
        };
    }
}

public sealed class DocumentUnlock
{
    private DocumentUnlock()
    {
    }

    public Guid Id { get; private set; }

    public Guid TaskId { get; private set; }

    public Guid DocumentId { get; private set; }

    internal static DocumentUnlock Create(Guid id, Guid taskId, Guid documentId)
    {
        if (documentId == Guid.Empty)
        {
            throw new ArgumentException("An unlock rule needs a document.", nameof(documentId));
        }

        return new DocumentUnlock
        {
            Id = id,
            TaskId = taskId,
            DocumentId = documentId,
        };
    }
}
