using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WeddingBank.Application.Clients;
using WeddingBank.Application.Identity;
using WeddingBank.Domain.Clients;
using WeddingBank.Domain.Identity;
using WeddingBank.Infrastructure.Identity;

namespace WeddingBank.Tests.Clients;

public sealed class ClientEndpointTests : IClassFixture<ClientApiFixture>
{
    private const string ClientPassword = "client-password-1894";
    private const string AdminPassword = "admin-password-1894";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly ClientApiFixture _fixture;

    public ClientEndpointTests(ClientApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Anonymous_callers_cannot_open_client_or_administrator_records()
    {
        using var client = _fixture.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/clients")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/client/relationships")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/client/relationships/{ClientApiFixture.JuulClientId}")).StatusCode);
    }

    [Fact]
    public async Task A_client_cannot_open_the_administrator_client_desk()
    {
        using var client = _fixture.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, "juul", ClientPassword)).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/admin/clients")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/admin/clients/{ClientApiFixture.JuulClientId}")).StatusCode);
    }

    [Fact]
    public async Task An_administrator_can_search_the_client_list()
    {
        using var client = _fixture.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client, "administrator", AdminPassword)).StatusCode);

        var listed = await client.GetFromJsonAsync<ClientListBody>(
            "/api/admin/clients?search=AJ-ABCD-EFGH&status=ACTIVE&page=1&pageSize=1", Json);

        Assert.NotNull(listed);
        Assert.True(listed.Total >= 2);
        Assert.Equal(1, listed.FilteredCount);
        Assert.Equal("AJ-ABCD-EFGH", listed.Items[0].ClientReference);
        Assert.Equal(1, listed.PageSize);
    }

    [Fact]
    public async Task A_client_sees_only_an_associated_relationship()
    {
        using var juul = _fixture.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(juul, "juul", ClientPassword)).StatusCode);
        var own = await juul.GetFromJsonAsync<OwnRelationshipBody[]>("/api/client/relationships", Json);
        var other = await juul.GetAsync($"/api/client/relationships/{ClientApiFixture.AmberClientId}");

        Assert.NotNull(own);
        Assert.Contains(own, relationship => relationship.ClientReference == "AJ-ABCD-EFGH" && relationship.EmailAddress == "juul-house@example.com");
        Assert.DoesNotContain(own, relationship => relationship.Id == ClientApiFixture.AmberClientId);
        Assert.Equal(HttpStatusCode.NotFound, other.StatusCode);

        using var solo = _fixture.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(solo, "solo", ClientPassword)).StatusCode);
        var none = await solo.GetFromJsonAsync<OwnRelationshipBody[]>("/api/client/relationships", Json);
        Assert.NotNull(none);
        Assert.Empty(none);
    }

    [Fact]
    public async Task Invalid_client_input_is_rejected_and_a_valid_change_is_audited()
    {
        using var admin = _fixture.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(admin, "administrator", AdminPassword)).StatusCode);

        var invalid = await SendAsync(admin, HttpMethod.Post, "/api/admin/clients", new { legalName = "Maison", emailAddress = "not-an-email", status = "ACTIVE" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        var created = await SendAsync(admin, HttpMethod.Post, "/api/admin/clients", new
        {
            legalName = "Maison Audit",
            emailAddress = "audit-only@example.com",
            city = "Lyon",
            status = "ACTIVE",
        });
        var createdBody = await created.Content.ReadFromJsonAsync<ClientDetailBody>(Json);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(createdBody);
        Assert.StartsWith("AJ-", createdBody.ClientReference, StringComparison.Ordinal);

        var patched = await SendAsync(admin, HttpMethod.Patch, $"/api/admin/clients/{createdBody.Id}", new { phoneNumber = "+33 4 00 00 00 00" });
        var patchedBody = await patched.Content.ReadFromJsonAsync<ClientDetailBody>(Json);
        Assert.Equal(HttpStatusCode.OK, patched.StatusCode);
        Assert.Equal("Maison Audit", patchedBody!.LegalName);
        Assert.Equal("+33 4 00 00 00 00", patchedBody.PhoneNumber);

        var statusOnPatch = await SendAsync(admin, HttpMethod.Patch, $"/api/admin/clients/{createdBody.Id}", new { status = "INACTIVE" });
        Assert.Equal(HttpStatusCode.BadRequest, statusOnPatch.StatusCode);

        var audit = await admin.GetStringAsync("/api/admin/audit");
        Assert.Contains("client.created", audit, StringComparison.Ordinal);
        Assert.Contains("client.updated", audit, StringComparison.Ordinal);
        Assert.DoesNotContain("audit-only@example.com", audit, StringComparison.Ordinal);
        Assert.DoesNotContain(AdminPassword, audit, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Linking_is_explicit_and_a_suspended_relationship_hides_contact_details()
    {
        using var admin = _fixture.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(admin, "administrator", AdminPassword)).StatusCode);
        var created = await SendAsync(admin, HttpMethod.Post, "/api/admin/clients", new
        {
            legalName = "Maison Fermee",
            emailAddress = "fermee@example.com",
            status = "ACTIVE",
        });
        var record = await created.Content.ReadFromJsonAsync<ClientDetailBody>(Json);
        Assert.NotNull(record);

        var linked = await SendAsync(admin, HttpMethod.Post, $"/api/admin/clients/{record.Id}/users", new { login = "guest" });
        var again = await SendAsync(admin, HttpMethod.Post, $"/api/admin/clients/{record.Id}/users", new { login = "guest" });
        Assert.Equal(HttpStatusCode.OK, linked.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);

        using var guest = _fixture.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(guest, "guest", ClientPassword)).StatusCode);
        var open = await guest.GetFromJsonAsync<OwnRelationshipBody>($"/api/client/relationships/{record.Id}", Json);
        Assert.Equal("fermee@example.com", open!.EmailAddress);

        var suspended = await SendAsync(admin, HttpMethod.Post, $"/api/admin/clients/{record.Id}/status", new { status = "SUSPENDED" });
        Assert.Equal(HttpStatusCode.OK, suspended.StatusCode);
        var closed = await guest.GetFromJsonAsync<OwnRelationshipBody>($"/api/client/relationships/{record.Id}", Json);
        Assert.Equal("SUSPENDED", closed!.Status);
        Assert.False(closed.Active);
        Assert.Null(closed.EmailAddress);

        var removed = await SendAsync(admin, HttpMethod.Delete, $"/api/admin/clients/{record.Id}/users/{ClientApiFixture.GuestId}");
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await guest.GetAsync($"/api/client/relationships/{record.Id}")).StatusCode);

        var audit = await admin.GetStringAsync("/api/admin/audit");
        Assert.Contains("client.user_linked", audit, StringComparison.Ordinal);
        Assert.Contains("client.user_unlinked", audit, StringComparison.Ordinal);
        Assert.Contains("client.status_changed", audit, StringComparison.Ordinal);
    }

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string login, string password)
    {
        return await SendAsync(client, HttpMethod.Post, "/api/auth/login", new { login, password });
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string path, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        request.Headers.TryAddWithoutValidation("Origin", "http://localhost:5173");
        request.Headers.TryAddWithoutValidation("X-CSRF-TOKEN", await CsrfAsync(client));
        return await client.SendAsync(request);
    }

    private static async Task<string> CsrfAsync(HttpClient client)
    {
        var body = await client.GetFromJsonAsync<CsrfResponse>("/api/auth/csrf", Json);
        return body?.Token ?? throw new InvalidOperationException("CSRF token was missing.");
    }

    private sealed record CsrfResponse(string? Token);

    private sealed record ClientListBody(int Total, int FilteredCount, int PageSize, ClientItem[] Items);

    private sealed record ClientItem(string ClientReference);

    private sealed record ClientDetailBody(Guid Id, string ClientReference, string LegalName, string? PhoneNumber);

    private sealed record OwnRelationshipBody(Guid Id, string ClientReference, string Status, bool Active, string? EmailAddress);
}

public sealed class ClientApiFixture : IDisposable
{
    public static readonly Guid JuulId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    public static readonly Guid AmberId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    public static readonly Guid GuestId = Guid.Parse("33333333-3333-4333-8333-333333333333");
    public static readonly Guid SoloId = Guid.Parse("77777777-7777-4777-8777-777777777777");
    public static readonly Guid AdminId = Guid.Parse("44444444-4444-4444-8444-444444444444");
    public static readonly Guid JuulClientId = Guid.Parse("55555555-5555-4555-8555-555555555555");
    public static readonly Guid AmberClientId = Guid.Parse("66666666-6666-4666-8666-666666666666");

    private readonly WebApplicationFactory<Program> _factory;

    public ClientApiFixture()
    {
        var identities = new InMemoryIdentityStore();
        var hasher = new IdentityPasswordHasher();
        var now = new DateTimeOffset(2026, 11, 4, 9, 0, 0, TimeSpan.Zero);
        identities.AddUser(User.Create(JuulId, "juul", "Juul", hasher.Hash("client-password-1894"), SecurityStamp.Create(), now, [new Role(RoleNames.ClientId, RoleNames.Client)]));
        identities.AddUser(User.Create(AmberId, "amber", "Amber", hasher.Hash("client-password-1894"), SecurityStamp.Create(), now, [new Role(RoleNames.ClientId, RoleNames.Client)]));
        identities.AddUser(User.Create(GuestId, "guest", "Guest", hasher.Hash("client-password-1894"), SecurityStamp.Create(), now, [new Role(RoleNames.ClientId, RoleNames.Client)]));
        identities.AddUser(User.Create(SoloId, "solo", "Solo", hasher.Hash("client-password-1894"), SecurityStamp.Create(), now, [new Role(RoleNames.ClientId, RoleNames.Client)]));
        identities.AddUser(User.Create(AdminId, "administrator", "Administrator", hasher.Hash("admin-password-1894"), SecurityStamp.Create(), now, [new Role(RoleNames.AdminId, RoleNames.Admin)]));

        var clients = new MemoryClientStore();
        clients.AddAccount(new AccountProfile(JuulId, "juul", "Juul", [RoleNames.Client]));
        clients.AddAccount(new AccountProfile(AmberId, "amber", "Amber", [RoleNames.Client]));
        clients.AddAccount(new AccountProfile(GuestId, "guest", "Guest", [RoleNames.Client]));
        clients.AddAccount(new AccountProfile(SoloId, "solo", "Solo", [RoleNames.Client]));
        clients.AddAccount(new AccountProfile(AdminId, "administrator", "Administrator", [RoleNames.Admin]));
        clients.AddClient(Client.Open(JuulClientId, "AJ-ABCD-EFGH", "Maison Juul", "Juul", "juul-house@example.com", null, null, null, null, "Paris", "France", ClientStatus.Active, now));
        clients.AddClient(Client.Open(AmberClientId, "AJ-WXYZ-2345", "Maison Amber", "Amber", "amber-house@example.com", null, null, null, null, "Lyon", "France", ClientStatus.Active, now));
        clients.AddLink(ClientUser.Link(JuulClientId, JuulId, now));
        clients.AddLink(ClientUser.Link(AmberClientId, AmberId, now));

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Auth:AllowedOrigins:0", "http://localhost:5173");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IIdentityStore>();
                services.AddSingleton<IIdentityStore>(identities);
                services.RemoveAll<IClientStore>();
                services.AddSingleton<IClientStore>(clients);
                services.RemoveAll<IClientReferenceSource>();
                services.AddSingleton<IClientReferenceSource>(new FixedReferences(
                    "AJ-KLMN-PQRS",
                    "AJ-2345-6789",
                    "AJ-HHHH-2222",
                    "AJ-JKLM-NPQR",
                    "AJ-STUV-WXYZ",
                    "AJ-3468-ABCD"));
            });
        });
    }

    public HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false,
        });
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    private sealed class InMemoryIdentityStore : IIdentityStore
    {
        private readonly List<User> _users = [];
        private readonly List<AuthSession> _sessions = [];
        private readonly List<AuditEvent> _audits = [];

        public void AddUser(User user) => _users.Add(user);

        public Task<User?> FindUserByLoginAsync(string login, CancellationToken cancellationToken) =>
            Task.FromResult(_users.SingleOrDefault(user => user.Login == login));

        public Task<User?> FindUserByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_users.SingleOrDefault(user => user.Id == id));

        public Task<AuthSession?> FindSessionAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_sessions.SingleOrDefault(session => session.Id == id));

        public void AddSession(AuthSession session) => _sessions.Add(session);

        public void AddAuditEvent(AuditEvent auditEvent) => _audits.Add(auditEvent);

        public Task<IReadOnlyList<AuditEntry>> ListRecentAuditAsync(int limit, CancellationToken cancellationToken)
        {
            IReadOnlyList<AuditEntry> entries = _audits
                .OrderByDescending(audit => audit.OccurredAt)
                .ThenByDescending(audit => audit.Id)
                .Take(limit)
                .Select(audit =>
                {
                    var actor = audit.ActorUserId is null ? null : _users.SingleOrDefault(user => user.Id == audit.ActorUserId);
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

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
