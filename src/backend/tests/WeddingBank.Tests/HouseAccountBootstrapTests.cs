using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WeddingBank.Domain.Identity;
using WeddingBank.Infrastructure.Identity;
using WeddingBank.Infrastructure.Persistence;

namespace WeddingBank.Tests;

public sealed class HouseAccountBootstrapTests
{
    [Fact]
    public async Task A_missing_house_account_is_created_and_an_existing_password_stays()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<WeddingBankDbContext>().UseSqlite(connection).Options;
        var now = new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

        await using var db = new WeddingBankDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var client = await db.Roles.SingleAsync(role => role.Name == RoleNames.Client);
        db.Users.Add(User.Create(
            Guid.CreateVersion7(),
            "Juul-V",
            "Juul",
            "already-set",
            "stamp",
            now,
            [client]));
        await db.SaveChangesAsync();

        var bootstrap = new HouseAccountBootstrap(db, TimeProvider.System, new IdentityPasswordHasher());
        await bootstrap.EnsureAsync(
        [
            new HouseAccountSeed("Juul-V", "Juul", "replacement-hash", RoleNames.Client),
            new HouseAccountSeed("Amber-D", "Amber", "amber-hash", RoleNames.Client),
        ]);

        var juul = await db.Users.SingleAsync(user => user.Login == "Juul-V");
        var amber = await db.Users.Include(user => user.Roles).ThenInclude(link => link.Role).SingleAsync(user => user.Login == "Amber-D");
        Assert.Equal("already-set", juul.PasswordHash);
        Assert.Null(juul.Password);
        Assert.Equal("amber-hash", amber.PasswordHash);
        Assert.Equal(RoleNames.Client, amber.Roles.Single().Role!.Name);
    }

    [Fact]
    public async Task A_known_password_is_kept_readable_when_the_hash_still_matches()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<WeddingBankDbContext>().UseSqlite(connection).Options;
        var hasher = new IdentityPasswordHasher();
        const string Password = "Local-Juul-1894-Geneva";

        await using var db = new WeddingBankDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var client = await db.Roles.SingleAsync(role => role.Name == RoleNames.Client);
        db.Users.Add(User.Create(
            Guid.CreateVersion7(),
            "Juul-V",
            "Juul",
            hasher.Hash(Password),
            "stamp",
            DateTimeOffset.UtcNow,
            [client]));
        await db.SaveChangesAsync();

        var bootstrap = new HouseAccountBootstrap(db, TimeProvider.System, hasher);
        await bootstrap.EnsureAsync(
        [
            new HouseAccountSeed("juul", "Juul", "unused-hash", RoleNames.Client, Password),
        ]);

        var juul = await db.Users.SingleAsync(user => user.Login == "Juul-V");
        Assert.Equal(Password, juul.Password);
        Assert.DoesNotContain(Password, juul.PasswordHash, StringComparison.Ordinal);
    }

    [Fact]
    public void The_production_connection_uses_the_database_password_without_replacing_an_explicit_string()
    {
        var configured = DatabaseConnection.Resolve(
            "Host=explicit",
            "secret-value",
            "postgres",
            "weddingbank",
            "weddingbank",
            isDevelopment: false);
        var built = DatabaseConnection.Resolve(
            null,
            "secret-value",
            "postgres",
            "weddingbank",
            "weddingbank",
            isDevelopment: false);

        Assert.Equal("Host=explicit", configured);
        Assert.Contains("Host=postgres", built, StringComparison.Ordinal);
        Assert.Contains("Password=secret-value", built, StringComparison.Ordinal);
        Assert.DoesNotContain("Host=invalid", built, StringComparison.Ordinal);
    }
}
