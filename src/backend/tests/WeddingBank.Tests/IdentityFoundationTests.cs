using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WeddingBank.Application.Identity;
using WeddingBank.Domain.Identity;
using WeddingBank.Infrastructure.Identity;
using WeddingBank.Infrastructure.Persistence;

namespace WeddingBank.Tests;

public sealed class IdentityFoundationTests
{
    [Fact]
    public void Five_failures_lock_the_account_for_fifteen_minutes()
    {
        var user = SampleUser();
        var now = new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);

        for (var attempt = 0; attempt < 4; attempt++)
        {
            user.RegisterFailedSignIn(now);
        }

        Assert.False(user.IsLocked(now));
        user.RegisterFailedSignIn(now);
        Assert.True(user.IsLocked(now.AddMinutes(14)));
        Assert.False(user.IsLocked(now.AddMinutes(15)));

        user.RegisterSuccessfulSignIn(now);
        Assert.False(user.IsLocked(now));
    }

    [Fact]
    public void A_session_ends_on_idle_time_absolute_time_stamp_change_or_logout()
    {
        var now = new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);
        var session = AuthSession.Issue(Guid.CreateVersion7(), Guid.CreateVersion7(), "stamp", now);

        Assert.True(session.IsActive(now.AddMinutes(29), "stamp"));
        Assert.False(session.IsActive(now.AddMinutes(30), "stamp"));
        Assert.False(session.IsActive(now, "other"));

        session.Slide(now.AddMinutes(20));
        Assert.True(session.IsActive(now.AddMinutes(49), "stamp"));
        Assert.False(session.IsActive(now.AddMinutes(50), "stamp"));

        var longSession = AuthSession.Issue(Guid.CreateVersion7(), Guid.CreateVersion7(), "stamp", now);
        longSession.Slide(now.AddHours(7).AddMinutes(50));
        Assert.True(longSession.IsActive(now.AddHours(8).AddMinutes(-1), "stamp"));
        Assert.False(longSession.IsActive(now.AddHours(8), "stamp"));

        session.Revoke(now);
        Assert.False(session.IsActive(now.AddMinutes(21), "stamp"));
    }

    [Fact]
    public void Audit_metadata_rejects_secrets()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.Throws<InvalidOperationException>(() => AuditEvent.Record(
            Guid.CreateVersion7(),
            now,
            null,
            AuditEvent.LoginFailed,
            "login",
            "juul",
            null,
            new Dictionary<string, string> { ["password"] = "hidden" }));
    }

    [Fact]
    public async Task The_relational_model_stores_a_hashed_password_and_the_client_role()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<WeddingBankDbContext>().UseSqlite(connection).Options;

        await using (var db = new WeddingBankDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
            var role = await db.Roles.SingleAsync(item => item.Name == RoleNames.Client);
            var hasher = new IdentityPasswordHasher();
            db.Users.Add(User.Create(
                Guid.CreateVersion7(),
                "juul",
                "Juul",
                hasher.Hash("Local-Juul-1894-Geneva"),
                SecurityStamp.Create(),
                DateTimeOffset.UtcNow,
                [role]));
            await db.SaveChangesAsync();
        }

        await using var verify = new WeddingBankDbContext(options);
        var found = await new EfIdentityStore(verify).FindUserByLoginAsync("juul", CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal("Juul", found.DisplayName);
        Assert.Equal([RoleNames.Client], found.RoleNames());
        Assert.DoesNotContain("Local-Juul-1894-Geneva", found.PasswordHash, StringComparison.Ordinal);
        Assert.True(new IdentityPasswordHasher().Verify(found.PasswordHash, "Local-Juul-1894-Geneva"));
    }

    private static User SampleUser()
    {
        return User.Create(
            Guid.CreateVersion7(),
            "juul",
            "Juul",
            "hash",
            "stamp",
            DateTimeOffset.UtcNow,
            [new Role(RoleNames.ClientId, RoleNames.Client)]);
    }
}
