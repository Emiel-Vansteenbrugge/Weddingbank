using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WeddingBank.Application.Identity;
using WeddingBank.Domain.Identity;
using WeddingBank.Infrastructure.Persistence;

namespace WeddingBank.Infrastructure.Identity;

public sealed record HouseAccountSeed(string Login, string DisplayName, string PasswordHash, string Role, string? Password = null);

public sealed class HouseAccountBootstrap(WeddingBankDbContext db, TimeProvider clock, IPasswordHasher passwords)
{
    public static readonly HouseAccountSeed[] Accounts =
    [
        new("juul", "Juul", "AQAAAAIAAYagAAAAEJ0gB3Q6Bp58zo1zxAUMzhGwOD8eag0DSWSI9ld+IQFt+gYzcfwqydiGe4nHMfSwKQ==", RoleNames.Client, "Local-Juul-1894-Geneva"),
        new("amber", "Amber", "AQAAAAIAAYagAAAAEIibCK5TQIOsnF1h2BUUF0MMgIcj50fLbkLyVQxHbDIm/gcEG7GFaD6ci4rrRcoxog==", RoleNames.Client, "Local-Amber-1894-Zurich"),
        new("administrator", "Administrator", "AQAAAAIAAYagAAAAEGGO5lhiny1DvmNAncOIs+044SHsctECCFsOtvpmDpYXg9bQWKEe7ImOHMwn8FZmfQ==", RoleNames.Admin, "Local-Admin-1894-House"),
    ];

    public Task EnsureAsync(CancellationToken cancellationToken = default)
    {
        return EnsureAsync(Accounts, cancellationToken);
    }

    public async Task EnsureAsync(IReadOnlyList<HouseAccountSeed> accounts, CancellationToken cancellationToken = default)
    {
        var roles = await db.Roles.ToDictionaryAsync(role => role.Name, cancellationToken);
        var created = false;

        foreach (var account in accounts)
        {
            var login = LoginName.Normalize(account.Login);
            if (login is null || string.IsNullOrWhiteSpace(account.PasswordHash))
            {
                throw new InvalidOperationException("A house account needs a login and a password hash.");
            }

            if (!roles.TryGetValue(account.Role, out var role))
            {
                throw new InvalidOperationException($"House account '{login}' needs the role {account.Role}.");
            }

            var existing = await db.Users.SingleOrDefaultAsync(user => user.Login == login, cancellationToken);
            if (existing is not null)
            {
                if (existing.Password is null
                    && account.Password is not null
                    && existing.PasswordHash.StartsWith("AQAAAAI", StringComparison.Ordinal)
                    && passwords.Verify(existing.PasswordHash, account.Password)
                    && existing.RememberPassword(account.Password))
                {
                    created = true;
                }

                continue;
            }

            db.Users.Add(User.Create(
                Guid.CreateVersion7(),
                login,
                account.DisplayName.Trim(),
                account.PasswordHash,
                SecurityStamp.Create(),
                clock.GetUtcNow(),
                [role],
                account.Password));
            created = true;
            Console.WriteLine($"Created house account {login}.");
        }

        if (created)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}

public sealed class ConfiguredHousePasswords(IOptions<AuthOptions> options) : IHousePasswordHint
{
    public string? For(string login)
    {
        foreach (var entry in options.Value.DevelopmentUsers)
        {
            if (string.Equals(entry.Login, login, StringComparison.OrdinalIgnoreCase) && entry.Password.Length > 0)
            {
                return entry.Password;
            }
        }

        foreach (var account in HouseAccountBootstrap.Accounts)
        {
            if (string.Equals(account.Login, login, StringComparison.Ordinal) && !string.IsNullOrEmpty(account.Password))
            {
                return account.Password;
            }
        }

        return null;
    }
}
