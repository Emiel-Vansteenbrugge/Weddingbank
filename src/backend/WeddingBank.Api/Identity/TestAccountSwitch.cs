using Microsoft.Extensions.Configuration;

namespace WeddingBank.Api.Identity;

// Temporary. Remove this gate, its endpoints, and the frontend test toolbar before the wedding.
internal static class TestAccountSwitch
{
    public const string EnabledKey = "TestAccounts:Enabled";

    public static bool Allowed(IConfiguration configuration)
    {
        return bool.TryParse(configuration[EnabledKey], out var enabled) && enabled;
    }
}
