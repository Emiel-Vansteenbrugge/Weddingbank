namespace WeddingBank.Application.Identity;

public static class SecurityStamp
{
    public static string Create()
    {
        return Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    }
}
