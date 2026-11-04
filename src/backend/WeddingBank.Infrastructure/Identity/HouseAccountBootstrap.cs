using Microsoft.EntityFrameworkCore;
using WeddingBank.Application.Identity;
using WeddingBank.Domain.Identity;
using WeddingBank.Infrastructure.Persistence;

namespace WeddingBank.Infrastructure.Identity;

public sealed record HouseAccountSeed(string Login, string DisplayName, string PasswordHash, string Role);

public sealed class HouseAccountBootstrap(WeddingBankDbContext db, TimeProvider clock)
{
    public static readonly HouseAccountSeed[] Accounts =
    [
        new("juul", "Juul", "AQAAAAIAAYagAAAAEJ0gB3Q6Bp58zo1zxAUMzhGwOD8eag0DSWSI9ld+IQFt+gYzcfwqydiGe4nHMfSwKQ==", RoleNames.Client),
        new("amber", "Amber", "AQAAAAIAAYagAAAAEIibCK5TQIOsnF1h2BUUF0MMgIcj50fLbkLyVQxHbDIm/gcEG7GFaD6ci4rrRcoxog==", RoleNames.Client),
        new("administrator", "Administrator", "AQAAAAIAAYagAAAAEGGO5lhiny1DvmNAncOIs+044SHsctECCFsOtvpmDpYXg9bQWKEe7ImOHMwn8FZmfQ==", RoleNames.Admin),
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

            if (await db.Users.AnyAsync(user => user.Login == login, cancellationToken))
            {
                continue;
            }

            db.Users.Add(User.Create(
                Guid.CreateVersion7(),
                login,
                account.DisplayName.Trim(),
                account.PasswordHash,
                SecurityStamp.Create(),
                clock.GetUtcNow(),
                [role]));
            created = true;
            Console.WriteLine($"Created house account {login}.");
        }

        if (created)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
