using WeddingBank.Domain.Game;

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

        var login = value.Trim();
        if (login.Length is < 1 or > MaxLength || login.Any(char.IsControl))
        {
            return null;
        }

        if (login.Equals("juul", StringComparison.OrdinalIgnoreCase) || login.Equals(Participants.Juul, StringComparison.OrdinalIgnoreCase))
        {
            return Participants.Juul;
        }

        if (login.Equals("amber", StringComparison.OrdinalIgnoreCase) || login.Equals(Participants.Amber, StringComparison.OrdinalIgnoreCase))
        {
            return Participants.Amber;
        }

        return login.ToLowerInvariant();
    }
}
