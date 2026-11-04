using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WeddingBank.Application.Identity;
using WeddingBank.Domain.Identity;
using WeddingBank.Infrastructure.Persistence;

namespace WeddingBank.Infrastructure.Identity;

public sealed class DevelopmentUserSeeder(
    WeddingBankDbContext db,
    IPasswordHasher passwords,
    IOptions<AuthOptions> options,
    TimeProvider clock)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var configured = options.Value.DevelopmentUsers;
        if (configured.Length == 0)
        {
            throw new InvalidOperationException("Auth:DevelopmentUsers is empty. Add the local accounts before seeding.");
        }

        var roles = await db.Roles.ToDictionaryAsync(role => role.Name, cancellationToken);
        if (roles.Count == 0)
        {
            throw new InvalidOperationException("Apply the identity migrations before seeding development users.");
        }

        foreach (var entry in configured)
        {
            var login = LoginName.Normalize(entry.Login);
            var displayName = entry.DisplayName.Trim();
            if (login is null || displayName.Length is < 1 or > 120)
            {
                throw new InvalidOperationException($"Development user '{entry.Login}' needs a login and a display name.");
            }

            if (entry.Password.Length < 12)
            {
                throw new InvalidOperationException($"Development user '{login}' needs a longer password.");
            }

            if (entry.Roles.Length == 0 || entry.Roles.Any(role => !roles.ContainsKey(role)))
            {
                throw new InvalidOperationException($"Development user '{login}' has a role that is not ADMIN or CLIENT.");
            }

            if (await db.Users.AnyAsync(user => user.Login == login, cancellationToken))
            {
                Console.WriteLine($"Skipped {login}; the account already exists.");
                continue;
            }

            var assigned = entry.Roles.Select(role => roles[role]).ToArray();
            var user = User.Create(
                Guid.CreateVersion7(),
                login,
                displayName,
                passwords.Hash(entry.Password),
                SecurityStamp.Create(),
                clock.GetUtcNow(),
                assigned,
                entry.Password);
            db.Users.Add(user);
            Console.WriteLine($"Created {string.Join(", ", entry.Roles)} account {login}.");
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
