namespace WeddingBank.Application.Game;

public sealed record CaseSummary(
    Guid Id,
    string Reference,
    string Title,
    string Summary,
    string Status,
    string Audience,
    int DisplayOrder,
    int DocumentCount,
    DateTimeOffset UpdatedAt);

public sealed record DocumentSummary(
    Guid Id,
    string Reference,
    string Title,
    string Category,
    string? Description,
    string Status,
    string Audience,
    int DisplayOrder,
    DateTimeOffset UpdatedAt);

public sealed record CaseDetail(
    Guid Id,
    string Reference,
    string Title,
    string Summary,
    string? Introduction,
    string Status,
    string Audience,
    int DisplayOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<DocumentSummary> Documents);

public sealed record DocumentDetail(
    Guid Id,
    Guid CaseId,
    string Reference,
    string Title,
    string Category,
    string? Description,
    string Content,
    string Status,
    string Audience,
    int DisplayOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ReadingCase(
    Guid Id,
    string Reference,
    string Title,
    string Summary,
    string? Introduction,
    IReadOnlyList<ReadingDocument> Documents);

public sealed record ReadingDocument(
    Guid Id,
    Guid CaseId,
    string CaseReference,
    string CaseTitle,
    string Reference,
    string Title,
    string Category,
    string? Description,
    int DisplayOrder,
    bool IsNew);

public sealed record ReadingPaper(
    Guid Id,
    Guid CaseId,
    string CaseReference,
    string CaseTitle,
    string Reference,
    string Title,
    string Category,
    string? Description,
    string Content,
    int DisplayOrder,
    bool IsNew);

public sealed record CaseDraft(string? Reference, string? Title, string? Summary, string? Introduction, string? Audience);

public sealed record DocumentDraft(
    string? Reference,
    string? Title,
    string? Category,
    string? Description,
    string? Content,
    string? Audience);
