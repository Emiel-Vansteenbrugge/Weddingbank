using WeddingBank.Domain.Identity;

namespace WeddingBank.Application.Identity;

public sealed class AuthenticationService(IIdentityStore store, IPasswordHasher passwords, TimeProvider clock)
{
    public async Task<SignedIn?> SignInAsync(string? login, string? password, string? ipAddress, CancellationToken cancellationToken)
    {
        var normalized = LoginName.Normalize(login);
        var submitted = password ?? "";
        if (normalized is null || submitted.Length is 0 or > 200)
        {
            passwords.VerifyUnknownUser(submitted.Length == 0 ? " " : submitted[..Math.Min(submitted.Length, 200)]);
            await RejectAsync(null, normalized, "credentials", ipAddress, cancellationToken);
            return null;
        }

        var now = clock.GetUtcNow();
        var user = await store.FindUserByLoginAsync(normalized, cancellationToken);
        if (user is null)
        {
            passwords.VerifyUnknownUser(submitted);
            await RejectAsync(null, normalized, "credentials", ipAddress, cancellationToken);
            return null;
        }

        if (user.IsLocked(now))
        {
            await RejectAsync(user.Id, null, "locked", ipAddress, cancellationToken);
            return null;
        }

        if (!passwords.Verify(user.PasswordHash, submitted))
        {
            user.RegisterFailedSignIn(now);
            await RejectAsync(user.Id, null, user.IsLocked(now) ? "locked" : "credentials", ipAddress, cancellationToken);
            return null;
        }

        user.RegisterSuccessfulSignIn(now);
        var session = AuthSession.Issue(Guid.CreateVersion7(), user.Id, user.SecurityStamp, now);
        store.AddSession(session);
        store.AddAuditEvent(AuditEvent.Record(
            Guid.CreateVersion7(),
            now,
            user.Id,
            AuditEvent.LoginSucceeded,
            "user",
            user.Id.ToString(),
            ipAddress,
            null));
        await store.SaveChangesAsync(cancellationToken);
        return new SignedIn(ToUser(user), session.Id, session.CreatedAt, session.AbsoluteExpiresAt);
    }

    public async Task<AuthUser?> ValidateSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await store.FindSessionAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        var user = await store.FindUserByIdAsync(session.UserId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var now = clock.GetUtcNow();
        if (!session.IsActive(now, user.SecurityStamp))
        {
            return null;
        }

        if (now - session.LastActivityAt >= TimeSpan.FromMinutes(1))
        {
            session.Slide(now);
            await store.SaveChangesAsync(cancellationToken);
        }

        return ToUser(user);
    }

    public async Task SignOutAsync(Guid? sessionId, string? ipAddress, CancellationToken cancellationToken)
    {
        if (sessionId is null)
        {
            return;
        }

        var session = await store.FindSessionAsync(sessionId.Value, cancellationToken);
        if (session is null || session.RevokedAt is not null)
        {
            return;
        }

        var now = clock.GetUtcNow();
        var user = await store.FindUserByIdAsync(session.UserId, cancellationToken);
        session.Revoke(now);
        user?.RotateSecurityStamp(SecurityStamp.Create(), now);
        store.AddAuditEvent(AuditEvent.Record(
            Guid.CreateVersion7(),
            now,
            user?.Id,
            AuditEvent.Logout,
            "user",
            user?.Id.ToString(),
            ipAddress,
            null));
        await store.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEntry>> ReadAuditAsync(Guid actorId, string? ipAddress, CancellationToken cancellationToken)
    {
        var entries = await store.ListRecentAuditAsync(100, cancellationToken);
        store.AddAuditEvent(AuditEvent.Record(
            Guid.CreateVersion7(),
            clock.GetUtcNow(),
            actorId,
            AuditEvent.AuditViewed,
            null,
            null,
            ipAddress,
            null));
        await store.SaveChangesAsync(cancellationToken);
        return entries;
    }

    private async Task RejectAsync(Guid? actorId, string? login, string reason, string? ipAddress, CancellationToken cancellationToken)
    {
        store.AddAuditEvent(AuditEvent.Record(
            Guid.CreateVersion7(),
            clock.GetUtcNow(),
            actorId,
            AuditEvent.LoginFailed,
            actorId is null ? "login" : "user",
            actorId?.ToString() ?? login,
            ipAddress,
            new Dictionary<string, string> { ["reason"] = reason }));
        await store.SaveChangesAsync(cancellationToken);
    }

    private static AuthUser ToUser(User user)
    {
        return new AuthUser(user.Id, user.Login, user.DisplayName, user.RoleNames());
    }
}
