namespace WeddingBank.Domain.Identity;

public sealed class User
{
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly List<UserRole> _roles = [];

    private User()
    {
    }

    public Guid Id { get; private set; }

    public string Login { get; private set; } = "";

    public string DisplayName { get; private set; } = "";

    public string PasswordHash { get; private set; } = "";

    public string SecurityStamp { get; private set; } = "";

    public int FailedSignInCount { get; private set; }

    public DateTimeOffset? LockoutUntil { get; private set; }

    /// <summary>
    /// Reserved for administrator TOTP. Sign-in does not require a code yet.
    /// Before production use, the secret must be protected with ASP.NET Data Protection
    /// and an administrator session must be withheld until the code is accepted.
    /// </summary>
    public bool TotpEnabled { get; private set; }

    public string? TotpSecretProtected { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<UserRole> Roles => _roles;

    public static User Create(
        Guid id,
        string login,
        string displayName,
        string passwordHash,
        string securityStamp,
        DateTimeOffset now,
        IEnumerable<Role> roles)
    {
        var user = new User
        {
            Id = id,
            Login = login,
            DisplayName = displayName,
            PasswordHash = passwordHash,
            SecurityStamp = securityStamp,
            CreatedAt = now,
            UpdatedAt = now,
        };

        foreach (var role in roles)
        {
            var link = new UserRole(id, role.Id);
            link.AttachRole(role);
            user._roles.Add(link);
        }

        return user;
    }

    public IReadOnlyList<string> RoleNames()
    {
        return _roles.Select(link => link.Role?.Name).OfType<string>().ToArray();
    }

    public bool IsLocked(DateTimeOffset now)
    {
        return LockoutUntil is not null && LockoutUntil > now;
    }

    public void RegisterFailedSignIn(DateTimeOffset now)
    {
        FailedSignInCount++;
        if (FailedSignInCount >= MaxFailedAttempts)
        {
            LockoutUntil = now.Add(LockoutDuration);
            FailedSignInCount = 0;
        }

        UpdatedAt = now;
    }

    public void RegisterSuccessfulSignIn(DateTimeOffset now)
    {
        FailedSignInCount = 0;
        LockoutUntil = null;
        UpdatedAt = now;
    }

    public void RotateSecurityStamp(string securityStamp, DateTimeOffset now)
    {
        SecurityStamp = securityStamp;
        UpdatedAt = now;
    }

    public void ReplacePassword(string passwordHash, string securityStamp, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(passwordHash) || string.IsNullOrWhiteSpace(securityStamp))
        {
            throw new ArgumentException("A password change needs a hash and a new security stamp.");
        }

        PasswordHash = passwordHash;
        SecurityStamp = securityStamp;
        FailedSignInCount = 0;
        LockoutUntil = null;
        UpdatedAt = now;
    }
}
