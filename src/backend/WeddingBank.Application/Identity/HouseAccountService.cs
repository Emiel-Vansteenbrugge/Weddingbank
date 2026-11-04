using WeddingBank.Application;
using WeddingBank.Domain.Game;
using WeddingBank.Domain.Identity;

namespace WeddingBank.Application.Identity;

public sealed record HouseAccount(string Login, string DisplayName, IReadOnlyList<string> Roles, string? Password);

public interface IHousePasswordHint
{
    string? For(string login);
}

public sealed class HouseAccountService(
    IIdentityStore identities,
    IPasswordHasher passwords,
    IHousePasswordHint hints,
    TimeProvider clock)
{
    public static readonly string[] Logins = [Participants.Juul, Participants.Amber, "administrator"];

    public async Task<IReadOnlyList<HouseAccount>> ListAsync(CancellationToken cancellationToken)
    {
        var accounts = new List<HouseAccount>();
        var remembered = false;
        foreach (var login in Logins)
        {
            var user = await identities.FindUserByLoginAsync(login, cancellationToken);
            if (user is null)
            {
                continue;
            }

            var known = hints.For(user.Login);
            if (user.Password is null
                && known is not null
                && user.PasswordHash.StartsWith("AQAAAAI", StringComparison.Ordinal)
                && passwords.Verify(user.PasswordHash, known)
                && user.RememberPassword(known))
            {
                remembered = true;
            }

            accounts.Add(new HouseAccount(user.Login, user.DisplayName, user.RoleNames(), user.Password));
        }

        if (remembered)
        {
            await identities.SaveChangesAsync(cancellationToken);
        }

        return accounts;
    }

    public async Task<OperationResult<Completed>> ChangePasswordAsync(
        string? login,
        string? password,
        string? confirmation,
        Guid actorId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var normalized = LoginName.Normalize(login);
        if (normalized is null || !Logins.Contains(normalized, StringComparer.Ordinal))
        {
            return OperationResult<Completed>.NotFound("That account is not one of the house sign-ins.");
        }

        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrEmpty(password) || password.Length < 12)
        {
            errors["password"] = ["Use at least 12 characters."];
        }
        else if (password.Length > 200)
        {
            errors["password"] = ["Use a shorter password."];
        }

        if (!string.Equals(password, confirmation, StringComparison.Ordinal))
        {
            errors["confirmation"] = ["Enter the same password again."];
        }

        if (errors.Count > 0)
        {
            return OperationResult<Completed>.Invalid(errors);
        }

        var user = await identities.FindUserByLoginAsync(normalized, cancellationToken);
        if (user is null)
        {
            return OperationResult<Completed>.NotFound("That account is not open.");
        }

        var now = clock.GetUtcNow();
        user.ReplacePassword(passwords.Hash(password!), password!, SecurityStamp.Create(), now);
        identities.AddAuditEvent(AuditEvent.Record(
            Guid.CreateVersion7(),
            now,
            actorId,
            AuditEvent.UserPasswordChanged,
            "user",
            user.Id.ToString(),
            ipAddress,
            new Dictionary<string, string> { ["login"] = user.Login }));
        await identities.SaveChangesAsync(cancellationToken);
        return OperationResult<Completed>.Ok(new Completed());
    }
}
