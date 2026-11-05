using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace WeddingBank.Api.Identity;

// Temporary. Remove this gate, its endpoints, and the frontend test toolbar before the wedding.
internal static class TestAccountSwitch
{
    public const string EnabledKey = "TestAccounts:Enabled";

    public static bool Allowed(IHostEnvironment environment, IConfiguration configuration)
    {
        return !environment.IsProduction()
            && bool.TryParse(configuration[EnabledKey], out var enabled)
            && enabled;
    }
}
