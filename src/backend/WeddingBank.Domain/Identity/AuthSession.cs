namespace WeddingBank.Domain.Identity;

public sealed class AuthSession
{
    public static readonly TimeSpan IdleLifetime = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan AbsoluteLifetime = TimeSpan.FromHours(8);

    private AuthSession()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string SecurityStamp { get; private set; } = "";

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset AbsoluteExpiresAt { get; private set; }

    public DateTimeOffset LastActivityAt { get; private set; }

    public DateTimeOffset IdleExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public static AuthSession Issue(Guid id, Guid userId, string securityStamp, DateTimeOffset now)
    {
        return new AuthSession
        {
            Id = id,
            UserId = userId,
            SecurityStamp = securityStamp,
            CreatedAt = now,
            AbsoluteExpiresAt = now.Add(AbsoluteLifetime),
            LastActivityAt = now,
            IdleExpiresAt = now.Add(IdleLifetime),
        };
    }

    public bool IsActive(DateTimeOffset now, string currentSecurityStamp)
    {
        return RevokedAt is null
            && SecurityStamp == currentSecurityStamp
            && now < AbsoluteExpiresAt
            && now < IdleExpiresAt;
    }

    public void Slide(DateTimeOffset now)
    {
        LastActivityAt = now;
        var idle = now.Add(IdleLifetime);
        IdleExpiresAt = idle < AbsoluteExpiresAt ? idle : AbsoluteExpiresAt;
    }

    public void Revoke(DateTimeOffset now)
    {
        RevokedAt ??= now;
    }
}
