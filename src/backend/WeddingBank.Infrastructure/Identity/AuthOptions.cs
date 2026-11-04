namespace WeddingBank.Infrastructure.Identity;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string[] AllowedOrigins { get; set; } = [];

    public DevelopmentUserOptions[] DevelopmentUsers { get; set; } = [];
}

public sealed class DevelopmentUserOptions
{
    public string Login { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string Password { get; set; } = "";

    public string[] Roles { get; set; } = [];
}
