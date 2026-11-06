namespace WeddingBank.Application.Game;

public sealed record DocumentLocks(IReadOnlySet<Guid> Ruled, IReadOnlySet<Guid> Hidden)
{
    public static DocumentLocks None { get; } = new(new HashSet<Guid>(), new HashSet<Guid>());
}

public interface IDocumentGate
{
    Task<DocumentLocks> LocksForAsync(string login, CancellationToken cancellationToken);
}

public sealed record QuestSummary(
    Guid Id,
    string Reference,
    string Title,
    string Status,
    string Audience,
    string Kind,
    Guid CaseId,
    string CaseReference,
    int DisplayOrder,
    int TaskCount,
    int CompletedAssignments,
    int OpenAssignments,
    DateTimeOffset UpdatedAt);

public sealed record UnlockDetail(Guid DocumentId, string Reference, string Title, bool Released);

public sealed record TaskDetail(
    Guid Id,
    string Reference,
    string Title,
    string Instructions,
    string TaskType,
    string Status,
    string Scope,
    string? ParticipantLogin,
    bool IgnoreCase,
    bool TrimWhitespace,
    string? SuccessMessage,
    int DisplayOrder,
    bool Completed,
    IReadOnlyList<string> Answers,
    IReadOnlyList<UnlockDetail> Unlocks,
    DateTimeOffset UpdatedAt);

public sealed record QuestDetail(
    Guid Id,
    string Reference,
    string Title,
    string Description,
    string Instructions,
    Guid CaseId,
    string CaseReference,
    string Status,
    string Audience,
    string Kind,
    int DisplayOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<TaskDetail> Tasks);

public sealed record SubmissionRecord(
    Guid Id,
    Guid TaskId,
    string TaskReference,
    string Scope,
    string? AssignedTo,
    string SubmittedBy,
    string Answer,
    string Outcome,
    DateTimeOffset SubmittedAt);

public sealed record ReadingQuestSummary(
    Guid Id,
    string Reference,
    string Title,
    string Description,
    int OpenTasks,
    int CompletedTasks);

public sealed record ReadingTask(
    Guid Id,
    string Reference,
    string Title,
    string Instructions,
    bool Shared,
    bool Completed,
    string? SuccessMessage);

public sealed record ReadingQuest(
    Guid Id,
    string Reference,
    string Title,
    string Description,
    string Instructions,
    Guid CaseId,
    string CaseReference,
    string CaseTitle,
    int SharedCompleted,
    int SharedOpen,
    IReadOnlyList<ReadingTask> Tasks);

public sealed record AnswerOutcome(bool Accepted, bool Completed, string Message);

public sealed record QuestDraft(
    string? Reference,
    string? Title,
    string? Description,
    string? Instructions,
    Guid? CaseId,
    string? Audience,
    string? Kind);

public sealed record TaskDraft(
    string? Reference,
    string? Title,
    string? Instructions,
    string? TaskType,
    string? Scope,
    string? Participant,
    IReadOnlyList<string>? Answers,
    bool? IgnoreCase,
    bool? TrimWhitespace,
    string? SuccessMessage,
    IReadOnlyList<Guid>? UnlockDocumentIds);
