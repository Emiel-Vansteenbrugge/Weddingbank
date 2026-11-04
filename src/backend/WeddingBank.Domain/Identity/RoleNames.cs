namespace WeddingBank.Domain.Identity;

public static class RoleNames
{
    public const string Admin = "ADMIN";
    public const string Client = "CLIENT";

    public static readonly Guid AdminId = Guid.Parse("4f1c0a6e-8b32-4d5a-9c17-2e6b8a0d4f31");
    public static readonly Guid ClientId = Guid.Parse("9a7e3c12-6d48-4f0b-8e25-1c9b7a6d5e40");
}
