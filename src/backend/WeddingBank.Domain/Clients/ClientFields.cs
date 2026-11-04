using System.Net.Mail;

namespace WeddingBank.Domain.Clients;

public static class ClientFields
{
    public const int LegalNameMax = 160;
    public const int DisplayNameMax = 160;
    public const int EmailMax = 200;
    public const int PhoneMax = 40;
    public const int AddressMax = 120;
    public const int PostalMax = 16;
    public const int CityMax = 80;
    public const int CountryMax = 80;

    public static string? OneLine(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var line = string.Join(' ', value.Split(['\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries)).Trim();
        return line.Length == 0 ? null : line;
    }

    public static bool IsEmail(string email)
    {
        return email.Length is >= 3 and <= EmailMax
            && !email.Contains(' ')
            && MailAddress.TryCreate(email, out var parsed)
            && parsed.Address == email;
    }
}
