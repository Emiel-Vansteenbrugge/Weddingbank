namespace WeddingBank.Application.Identity;

public sealed record AuthUser(Guid Id, string Login, string DisplayName, IReadOnlyList<string> Roles);

public sealed record SignedIn(AuthUser User, Guid SessionId, DateTimeOffset IssuedAt, DateTimeOffset ExpiresAt);

public sealed record AuditEntry(
    Guid Id,
    DateTimeOffset OccurredAt,
    Guid? ActorUserId,
    string? ActorLogin,
    string EventType,
    string? TargetType,
    string? TargetId,
    string? IpAddress,
    string? MetadataJson);
