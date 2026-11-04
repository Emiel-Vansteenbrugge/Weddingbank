using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WeddingBank.Application.Game;
using WeddingBank.Application.Identity;
using WeddingBank.Domain.Identity;
using WeddingBank.Infrastructure.Identity;

namespace WeddingBank.Tests;

public sealed class AuthEndpointTests : IClassFixture<AuthApiFixture>
{
    private const string ClientPassword = "client-password-1894";
    private const string AdminPassword = "admin-password-1894";
    private readonly AuthApiFixture _fixture;

    public AuthEndpointTests(AuthApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Sign_in_returns_the_client_without_the_password()
    {
        using var client = _fixture.CreateClient();

        var response = await LoginAsync(client, "juul", ClientPassword);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Juul", body, StringComparison.Ordinal);
        Assert.Contains("CLIENT", body, StringComparison.Ordinal);
        Assert.DoesNotContain("ADMIN", body, StringComparison.Ordinal);
        Assert.DoesNotContain(ClientPassword, body, StringComparison.Ordinal);
        Assert.Contains("wb.session=", string.Join(' ', response.Headers.GetValues("Set-Cookie")), StringComparison.Ordinal);

        var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Contains("juul", await me.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Failed_sign_in_is_generic_and_reaches_the_audit_log()
    {
        using var client = _fixture.CreateClient();
        var wrong = await LoginAsync(client, "juul", "wrong-password-1894");
        var unknown = await LoginAsync(client, "nobody", "wrong-password-1894");
        var wrongBody = await wrong.Content.ReadAsStringAsync();
        var unknownBody = await unknown.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Equal(wrongBody, unknownBody);
        Assert.Contains("not recognised", wrongBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("wrong-password-1894", wrongBody, StringComparison.Ordinal);

        using var admin = _fixture.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(admin, "administrator", AdminPassword)).StatusCode);
        var audit = await admin.GetStringAsync("/api/admin/audit");
        Assert.Contains("login.failed", audit, StringComparison.Ordinal);
        Assert.DoesNotContain("wrong-password-1894", audit, StringComparison.Ordinal);
        Assert.DoesNotContain(AdminPassword, audit, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Logout_invalidates_the_issued_session()
    {
        using var client = _fixture.CreateClient();
        var login = await LoginAsync(client, "juul", ClientPassword);
        var session = login.Headers.GetValues("Set-Cookie")
            .First(value => value.StartsWith("wb.session=", StringComparison.Ordinal))
            .Split(';', 2)[0];

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await PostAsync(client, "/api/auth/logout")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);

        using var replay = _fixture.CreateClient(handleCookies: false);
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.TryAddWithoutValidation("Cookie", session);
        var response = await replay.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_requires_the_house_origin_and_a_csrf_token()
    {
        using var client = _fixture.CreateClient();
        var token = await CsrfAsync(client);

        var missingOrigin = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { login = "juul", password = ClientPassword }),
        };
        missingOrigin.Headers.TryAddWithoutValidation("X-CSRF-TOKEN", token);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(missingOrigin)).StatusCode);

        var missingToken = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { login = "juul", password = ClientPassword }),
        };
        missingToken.Headers.TryAddWithoutValidation("Origin", "http://localhost:5173");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(missingToken)).StatusCode);
    }

    [Fact]
    public async Task Anonymous_calls_are_refused()
    {
        using var client = _fixture.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/client/desk")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/game")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/audit")).StatusCode);
    }

    [Fact]
    public async Task A_client_cannot_open_the_administrator_desk()
    {
        using var client = _fixture.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, "juul", ClientPassword)).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/game")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/audit")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/client/desk")).StatusCode);
    }

    [Fact]
    public async Task An_administrator_cannot_open_the_client_record()
    {
        using var client = _fixture.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, "administrator", AdminPassword)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/admin/game")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/client/desk")).StatusCode);
    }

    [Fact]
    public async Task An_administrator_can_replace_a_house_password_without_recording_it()
    {
        const string Replacement = "replacement-password-1894";
        using var admin = _fixture.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(admin, "administrator", AdminPassword)).StatusCode);

        var accounts = await admin.GetStringAsync("/api/admin/house-accounts");
        Assert.Contains("juul", accounts, StringComparison.Ordinal);
        Assert.Contains("amber", accounts, StringComparison.Ordinal);
        Assert.Contains("administrator", accounts, StringComparison.Ordinal);

        try
        {
            var changed = await SendAsync(admin, "/api/admin/house-accounts/juul/password", new
            {
                password = Replacement,
                confirmation = Replacement,
            });
            Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);

            using var juul = _fixture.CreateClient();
            Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(juul, "juul", ClientPassword)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await LoginAsync(juul, "juul", Replacement)).StatusCode);

            var audit = await admin.GetStringAsync("/api/admin/audit");
            Assert.Contains("user.password_changed", audit, StringComparison.Ordinal);
            Assert.DoesNotContain(Replacement, audit, StringComparison.Ordinal);
            Assert.DoesNotContain(AdminPassword, audit, StringComparison.Ordinal);
        }
        finally
        {
            var restored = await SendAsync(admin, "/api/admin/house-accounts/juul/password", new
            {
                password = ClientPassword,
                confirmation = ClientPassword,
            });
            Assert.Equal(HttpStatusCode.NoContent, restored.StatusCode);
        }
    }

    [Fact]
    public async Task A_short_or_unconfirmed_password_is_refused_and_a_client_cannot_set_one()
    {
        using var admin = _fixture.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(admin, "administrator", AdminPassword)).StatusCode);
        var mismatch = await SendAsync(admin, "/api/admin/house-accounts/amber/password", new
        {
            password = "replacement-password-1894",
            confirmation = "different-password-1894",
        });
        var shortPassword = await SendAsync(admin, "/api/admin/house-accounts/administrator/password", new
        {
            password = "too-short",
            confirmation = "too-short",
        });
        Assert.Equal(HttpStatusCode.BadRequest, mismatch.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, shortPassword.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(admin, "/api/admin/house-accounts/guest/password", new
        {
            password = "replacement-password-1894",
            confirmation = "replacement-password-1894",
        })).StatusCode);

        using var client = _fixture.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, "juul", ClientPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/house-accounts")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _fixture.CreateClient().GetAsync("/api/admin/house-accounts")).StatusCode);
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, string path, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body),
        };
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

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
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

public sealed class AuthApiFixture : IDisposable
{
    private const string ClientPassword = "client-password-1894";
    private const string AdminPassword = "admin-password-1894";

    public AuthApiFixture()
    {
        var store = new InMemoryIdentityStore();
        var hasher = new IdentityPasswordHasher();
        var now = DateTimeOffset.UtcNow;
        store.AddUser(User.Create(
            Guid.CreateVersion7(),
            "juul",
            "Juul",
            hasher.Hash(ClientPassword),
            SecurityStamp.Create(),
            now,
            [new Role(RoleNames.ClientId, RoleNames.Client)]));
        store.AddUser(User.Create(
            Guid.CreateVersion7(),
            "amber",
            "Amber",
            hasher.Hash(ClientPassword),
            SecurityStamp.Create(),
            now,
            [new Role(RoleNames.ClientId, RoleNames.Client)]));
        store.AddUser(User.Create(
            Guid.CreateVersion7(),
            "administrator",
            "Administrator",
            hasher.Hash(AdminPassword),
            SecurityStamp.Create(),
            now,
            [new Role(RoleNames.AdminId, RoleNames.Admin)]));

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Auth:AllowedOrigins:0", "http://localhost:5173");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IIdentityStore>();
                services.AddSingleton<IIdentityStore>(store);
                services.RemoveAll<IGameStore>();
                services.AddSingleton<IGameStore, EmptyGameStore>();
            });
        });
    }

    private WebApplicationFactory<Program> Factory { get; }

    public HttpClient CreateClient(bool handleCookies = true)
    {
        return Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = handleCookies,
            AllowAutoRedirect = false,
        });
    }

    public void Dispose()
    {
        Factory.Dispose();
    }

    private sealed class EmptyGameStore : IGameStore
    {
        public Task<ProgressCounts> ReadCountsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new ProgressCounts(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, null, null));
        }
    }

    private sealed class InMemoryIdentityStore : IIdentityStore
    {
        private readonly List<User> _users = [];
        private readonly List<AuthSession> _sessions = [];
        private readonly List<AuditEvent> _audits = [];

        public void AddUser(User user)
        {
            _users.Add(user);
        }

        public Task<User?> FindUserByLoginAsync(string login, CancellationToken cancellationToken)
        {
            return Task.FromResult(_users.SingleOrDefault(user => user.Login == login));
        }

        public Task<User?> FindUserByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(_users.SingleOrDefault(user => user.Id == id));
        }

        public Task<AuthSession?> FindSessionAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(_sessions.SingleOrDefault(session => session.Id == id));
        }

        public void AddSession(AuthSession session)
        {
            _sessions.Add(session);
        }

        public void AddAuditEvent(AuditEvent auditEvent)
        {
            _audits.Add(auditEvent);
        }

        public Task<IReadOnlyList<AuditEntry>> ListRecentAuditAsync(int limit, CancellationToken cancellationToken)
        {
            IReadOnlyList<AuditEntry> entries = _audits
                .OrderByDescending(audit => audit.OccurredAt)
                .ThenByDescending(audit => audit.Id)
                .Take(limit)
                .Select(audit =>
                {
                    var actor = audit.ActorUserId is null
                        ? null
                        : _users.SingleOrDefault(user => user.Id == audit.ActorUserId);
                    return new AuditEntry(
                        audit.Id,
                        audit.OccurredAt,
                        audit.ActorUserId,
                        actor?.Login,
                        audit.EventType,
                        audit.TargetType,
                        audit.TargetId,
                        audit.IpAddress,
                        audit.MetadataJson);
                })
                .ToArray();
            return Task.FromResult(entries);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
