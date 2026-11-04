using Microsoft.AspNetCore.Identity;
using WeddingBank.Application.Identity;

namespace WeddingBank.Infrastructure.Identity;

public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();
    private readonly string _unknownUserHash;

    public IdentityPasswordHasher()
    {
        _unknownUserHash = _hasher.HashPassword(new object(), "unknown-user-timing-equalizer");
    }

    public string Hash(string password)
    {
        return _hasher.HashPassword(new object(), password);
    }

    public bool Verify(string passwordHash, string password)
    {
        var result = _hasher.VerifyHashedPassword(new object(), passwordHash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }

    public void VerifyUnknownUser(string password)
    {
        Verify(_unknownUserHash, password);
    }
}
