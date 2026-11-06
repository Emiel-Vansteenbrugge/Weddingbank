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

public sealed class GameEndpointTests : IClassFixture<GameApiFixture>
{
    private readonly GameApiFixture _fixture;

    public GameEndpointTests(GameApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task The_desk_shows_the_two_participants_and_refuses_client_administration()
    {
        using var admin = _fixture.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(admin, "administrator", GameApiFixture.AdminPassword)).StatusCode);

        var overview = await admin.GetFromJsonAsync<OverviewBody>("/api/admin/game");
        Assert.NotNull(overview);
        Assert.Equal("NOT_STARTED", overview.Status);
        Assert.Equal("Juul-V", overview.Juul.Login);
        Assert.Equal("Amber-D", overview.Amber.Login);
        Assert.Equal("Not started", overview.Juul.Stage);
        Assert.Equal("Not started", overview.Shared.Stage);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/api/admin/game/participants/administrator")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsync("/api/admin/clients", JsonContent.Create(new { }))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/api/client/desk")).StatusCode);

        using var juul = _fixture.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(juul, "juul", GameApiFixture.ClientPassword)).StatusCode);
        var desk = await juul.GetFromJsonAsync<DeskBody>("/api/client/desk");
        Assert.NotNull(desk);
        Assert.Equal("Juul-V", desk.Login);
        Assert.Equal("Juul", desk.Account.Holder);
        Assert.Equal("Private account", desk.Account.Title);
        Assert.DoesNotContain("Amber", desk.Account.Holder, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.Forbidden, (await juul.GetAsync("/api/admin/game")).StatusCode);

        using var amber = _fixture.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(amber, "amber", GameApiFixture.ClientPassword)).StatusCode);
        var amberDesk = await amber.GetFromJsonAsync<DeskBody>("/api/client/desk");
        Assert.Equal("Amber", amberDesk!.Account.Holder);
        Assert.DoesNotContain("Juul", amberDesk.Account.Holder, StringComparison.Ordinal);
    }

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string login, string password)
    {
        var token = await client.GetFromJsonAsync<CsrfResponse>("/api/auth/csrf");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { login, password }),
        };
        request.Headers.TryAddWithoutValidation("Origin", "http://localhost:5173");
        request.Headers.TryAddWithoutValidation("X-CSRF-TOKEN", token?.Token);
        return await client.SendAsync(request);
    }

    private sealed record CsrfResponse(string? Token);

    private sealed record OverviewBody(string Status, ParticipantBody Juul, ParticipantBody Amber, SharedBody Shared);

    private sealed record ParticipantBody(string Login, string Stage);

    private sealed record SharedBody(string Stage);

    private sealed record DeskBody(string Login, AccountBody Account);

    private sealed record AccountBody(string Holder, string Title);
}

public sealed class GameApiFixture : IDisposable
{
    public const string ClientPassword = "client-password-1894";
    public const string AdminPassword = "admin-password-1894";

    private readonly WebApplicationFactory<Program> _factory;

    public GameApiFixture()
    {
        var store = new InMemoryIdentityStore();
        var hasher = new IdentityPasswordHasher();
        var now = DateTimeOffset.UtcNow;
        store.Add(User.Create(Guid.CreateVersion7(), "Juul-V", "Juul", hasher.Hash(ClientPassword), SecurityStamp.Create(), now, [new Role(RoleNames.ClientId, RoleNames.Client)]));
        store.Add(User.Create(Guid.CreateVersion7(), "Amber-D", "Amber", hasher.Hash(ClientPassword), SecurityStamp.Create(), now, [new Role(RoleNames.ClientId, RoleNames.Client)]));
        store.Add(User.Create(Guid.CreateVersion7(), "administrator", "Administrator", hasher.Hash(AdminPassword), SecurityStamp.Create(), now, [new Role(RoleNames.AdminId, RoleNames.Admin)]));

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Auth:AllowedOrigins:0", "http://localhost:5173");
            builder.UseSetting("Documents:DeferReadBaseline", "true");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IIdentityStore>();
                services.AddSingleton<IIdentityStore>(store);
                services.RemoveAll<IGameStore>();
                services.AddSingleton<IGameStore>(new EmptyGameStore());
            });
        });
    }

    public HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true, AllowAutoRedirect = false });
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private sealed class EmptyGameStore : IGameStore
    {
        public Task<ProgressCounts> ReadCountsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new ProgressCounts(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, null, null));
        }

        public Task<VisibleProgress> ReadVisibleProgressAsync(string login, CancellationToken cancellationToken)
        {
            return Task.FromResult(new VisibleProgress(0, 0, 0, 0));
        }
    }

    private sealed class InMemoryIdentityStore : IIdentityStore
    {
        private readonly List<User> _users = [];
        private readonly List<AuthSession> _sessions = [];
        private readonly List<AuditEvent> _audits = [];

        public void Add(User user) => _users.Add(user);

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

        public void AddSession(AuthSession session) => _sessions.Add(session);

        public void AddAuditEvent(AuditEvent auditEvent) => _audits.Add(auditEvent);

        public Task<IReadOnlyList<AuditEntry>> ListRecentAuditAsync(int limit, CancellationToken cancellationToken)
        {
            IReadOnlyList<AuditEntry> entries = [];
            return Task.FromResult(entries);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
