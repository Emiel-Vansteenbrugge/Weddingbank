using System.Text.Json;

namespace WeddingBank.Domain.Identity;

public sealed class AuditEvent
{
    public const string LoginSucceeded = "login.succeeded";
    public const string LoginFailed = "login.failed";
    public const string Logout = "logout";
    public const string AuditViewed = "admin.audit.viewed";
    public const string ClientCreated = "client.created";
    public const string ClientUpdated = "client.updated";
    public const string ClientStatusChanged = "client.status_changed";
    public const string ClientUserLinked = "client.user_linked";
    public const string ClientUserUnlinked = "client.user_unlinked";
    public const string UserPasswordChanged = "user.password_changed";
    public const string ParticipantOpened = "admin.participant_opened";

    public const string TestAccountSwitched = "test.account_switched";

    private AuditEvent()
    {
    }

    public Guid Id { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public string EventType { get; private set; } = "";

    public string? TargetType { get; private set; }

    public string? TargetId { get; private set; }

    public string? IpAddress { get; private set; }

    public string? MetadataJson { get; private set; }

    public static AuditEvent Record(
        Guid id,
        DateTimeOffset occurredAt,
        Guid? actorUserId,
        string eventType,
        string? targetType,
        string? targetId,
        string? ipAddress,
        IReadOnlyDictionary<string, string>? metadata)
    {
        if (string.IsNullOrWhiteSpace(eventType) || eventType.Length > 80)
        {
            throw new ArgumentException("Audit event type is missing or too long.", nameof(eventType));
        }

        return new AuditEvent
        {
            Id = id,
            OccurredAt = occurredAt,
            ActorUserId = actorUserId,
            EventType = eventType,
            TargetType = Bound(targetType, 80),
            TargetId = Bound(targetId, 80),
            IpAddress = Bound(ipAddress, 64),
            MetadataJson = Serialize(metadata),
        };
    }

    private static string? Bound(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : null;
    }

    private static string? Serialize(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return null;
        }

        foreach (var (key, value) in metadata)
        {
            if (key.Contains("password", StringComparison.OrdinalIgnoreCase)
                || key.Contains("secret", StringComparison.OrdinalIgnoreCase)
                || key.Contains("token", StringComparison.OrdinalIgnoreCase)
                || key.Contains("cookie", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Audit metadata cannot contain secrets.");
            }

            if (value.Length > 200)
            {
                throw new InvalidOperationException("Audit metadata values must stay short.");
            }
        }

        return JsonSerializer.Serialize(metadata);
    }
}
