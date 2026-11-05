using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using WeddingBank.Api.Identity;

namespace WeddingBank.Tests;

public sealed class TestAccountSwitchTests
{
    [Fact]
    public void Production_rejects_the_switcher_even_when_the_flag_is_set()
    {
        var enabled = Config(true);

        Assert.False(TestAccountSwitch.Allowed(new NamedEnvironment(Environments.Production), enabled));
        Assert.False(TestAccountSwitch.Allowed(new NamedEnvironment(Environments.Development), Config(false)));
        Assert.True(TestAccountSwitch.Allowed(new NamedEnvironment(Environments.Development), enabled));
    }

    private static IConfiguration Config(bool enabled)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [TestAccountSwitch.EnabledKey] = enabled ? "true" : "false",
            })
            .Build();
    }

    private sealed class NamedEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;

        public string ApplicationName { get; set; } = "WeddingBank.Api";

        public string ContentRootPath { get; set; } = ".";

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

public sealed class TestAccountSwitchEndpointTests : IClassFixture<EnabledTestAccountFixture>
{
    private const string ClientPassword = "client-password-1894";
    private const string AdminPassword = "admin-password-1894";
    private readonly EnabledTestAccountFixture _fixture;

    public TestAccountSwitchEndpointTests(EnabledTestAccountFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Switching_uses_the_existing_account_and_leaves_the_password_unchanged()
    {
        using var client = _fixture.CreateClient();
        var listed = await client.GetStringAsync("/api/auth/test-accounts");
        Assert.Contains("Juul", listed, StringComparison.Ordinal);
        Assert.Contains("Amber", listed, StringComparison.Ordinal);
        Assert.Contains("Administrator", listed, StringComparison.Ordinal);
        Assert.DoesNotContain(ClientPassword, listed, StringComparison.Ordinal);
        Assert.DoesNotContain(AdminPassword, listed, StringComparison.Ordinal);

        var juul = await PostAsync(client, "/api/auth/test-accounts/juul/session");
        var juulBody = await juul.Content.ReadAsStringAsync();
        var juulSession = juul.Headers.GetValues("Set-Cookie")
            .First(value => value.StartsWith("wb.session=", StringComparison.Ordinal))
            .Split(';', 2)[0];
        Assert.Equal(HttpStatusCode.OK, juul.StatusCode);
        Assert.Contains("Juul-V", juulBody, StringComparison.Ordinal);
        Assert.Contains("CLIENT", juulBody, StringComparison.Ordinal);
        Assert.DoesNotContain("ADMIN", juulBody, StringComparison.Ordinal);
        Assert.DoesNotContain(ClientPassword, juulBody, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/house-accounts")).StatusCode);

        var admin = await PostAsync(client, "/api/auth/test-accounts/administrator/session");
        var adminBody = await admin.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
        Assert.Contains("ADMIN", adminBody, StringComparison.Ordinal);
        Assert.DoesNotContain(AdminPassword, adminBody, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/admin/house-accounts")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PostAsync(client, "/api/auth/test-accounts/guest/session")).StatusCode);

        using var replay = _fixture.CreateClient(handleCookies: false);
        var stale = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        stale.Headers.TryAddWithoutValidation("Cookie", juulSession);
        Assert.Equal(HttpStatusCode.Unauthorized, (await replay.SendAsync(stale)).StatusCode);

        using var password = _fixture.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(password, "juul", ClientPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(password, "administrator", AdminPassword)).StatusCode);
        var audit = await password.GetStringAsync("/api/admin/audit");
        Assert.Contains("test.account_switched", audit, StringComparison.Ordinal);
        Assert.DoesNotContain(ClientPassword, audit, StringComparison.Ordinal);
        Assert.DoesNotContain(AdminPassword, audit, StringComparison.Ordinal);
    }

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.TryAddWithoutValidation("Origin", "http://localhost:5173");
        request.Headers.TryAddWithoutValidation("X-CSRF-TOKEN", await CsrfAsync(client));
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string login, string password)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { login, password }),
        };
        request.Headers.TryAddWithoutValidation("Origin", "http://localhost:5173");
        request.Headers.TryAddWithoutValidation("X-CSRF-TOKEN", await CsrfAsync(client));
        return await client.SendAsync(request);
    }

    private static async Task<string> CsrfAsync(HttpClient client)
    {
        var body = await client.GetFromJsonAsync<CsrfResponse>("/api/auth/csrf");
        return body?.Token ?? throw new InvalidOperationException("CSRF token was missing.");
    }

    private sealed record CsrfResponse(string? Token);
}

public sealed class EnabledTestAccountFixture() : AuthApiFixture(true);
