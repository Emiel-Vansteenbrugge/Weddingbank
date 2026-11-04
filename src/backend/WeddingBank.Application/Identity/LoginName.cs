namespace WeddingBank.Application.Identity;

public static class LoginName
{
    public const int MaxLength = 80;

    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var login = value.Trim().ToLowerInvariant();
        if (login.Length is < 1 or > MaxLength || login.Any(char.IsControl))
        {
            return null;
        }

        return login;
    }
}
