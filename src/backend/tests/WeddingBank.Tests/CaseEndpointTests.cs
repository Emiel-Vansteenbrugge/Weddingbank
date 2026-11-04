using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WeddingBank.Application.Identity;
using WeddingBank.Domain.Game;
using WeddingBank.Domain.Identity;
using WeddingBank.Infrastructure.Identity;
using WeddingBank.Infrastructure.Persistence;

namespace WeddingBank.Tests;

public sealed class CaseEndpointTests : IClassFixture<CaseApiFixture>
{
    private readonly CaseApiFixture _fixture;

    public CaseEndpointTests(CaseApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task The_administrator_creates_revises_and_orders_cases()
    {
        using var admin = await _fixture.SignInAsync("administrator", CaseApiFixture.AdminPassword);
        var first = await CreateCaseAsync(admin, "ARCHIVE-2019-047", "The first file", "JUUL");
        var second = await CreateCaseAsync(admin, "ARCHIVE-2019-048", "The second file", "BOTH");

        var revised = await SendAsync<CaseBody>(admin, HttpMethod.Put, $"/api/admin/cases/{first.Id}", new
        {
            reference = "ARCHIVE-2019-047",
            title = "The revised file",
            summary = "A revised summary.",
            introduction = "Read this first.",
            audience = "JUUL",
        });
        Assert.Equal(HttpStatusCode.OK, revised.Status);
        Assert.Equal("The revised file", revised.Body!.Title);

        var published = await SendAsync<CaseBody>(admin, HttpMethod.Post, $"/api/admin/cases/{first.Id}/status", new { status = "PUBLISHED" });
        Assert.Equal("PUBLISHED", published.Body!.Status);
        var archived = await SendAsync<CaseBody>(admin, HttpMethod.Post, $"/api/admin/cases/{first.Id}/status", new { status = "ARCHIVED" });
        Assert.Equal("ARCHIVED", archived.Body!.Status);
        var draft = await SendAsync<CaseBody>(admin, HttpMethod.Post, $"/api/admin/cases/{first.Id}/status", new { status = "DRAFT" });
        Assert.Equal("DRAFT", draft.Body!.Status);

        var listed = await admin.GetFromJsonAsync<CaseBody[]>("/api/admin/cases");
        var ids = listed!.Select(item => item.Id).ToList();
        ids.Remove(second.Id);
        ids.Insert(ids.IndexOf(first.Id), second.Id);
        var ordered = await SendAsync<CaseBody[]>(admin, HttpMethod.Post, "/api/admin/cases/order", new { ids });
        Assert.Equal(HttpStatusCode.OK, ordered.Status);
        Assert.True(Array.FindIndex(ordered.Body!, item => item.Id == second.Id) < Array.FindIndex(ordered.Body!, item => item.Id == first.Id));

        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/api/client/cases")).StatusCode);
    }

    [Fact]
    public async Task A_duplicate_case_reference_is_rejected()
    {
        using var admin = await _fixture.SignInAsync("administrator", CaseApiFixture.AdminPassword);
        await CreateCaseAsync(admin, "LEDGER-1904-001", "Ledger", "BOTH");
        var duplicate = await SendAsync<CaseBody>(admin, HttpMethod.Post, "/api/admin/cases", CasePayload("ledger-1904-001", "Another", "AMBER"));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.Status);
    }

    [Fact]
    public async Task Documents_belong_to_a_case_and_keep_a_unique_reference()
    {
        using var admin = await _fixture.SignInAsync("administrator", CaseApiFixture.AdminPassword);
        var file = await CreateCaseAsync(admin, "MEMO-1912-003", "Internal memoranda", "JUUL");
        var missing = await SendAsync<DocumentBody>(admin, HttpMethod.Post, $"/api/admin/cases/{Guid.CreateVersion7()}/documents", DocumentPayload("MEMO-1", "BOTH"));
        Assert.Equal(HttpStatusCode.NotFound, missing.Status);

        var created = await SendAsync<DocumentBody>(admin, HttpMethod.Post, $"/api/admin/cases/{file.Id}/documents", DocumentPayload("MEMO-1", "JUUL"));
        Assert.Equal(HttpStatusCode.Created, created.Status);
        Assert.Equal("DRAFT", created.Body!.Status);

        var duplicate = await SendAsync<DocumentBody>(admin, HttpMethod.Post, $"/api/admin/cases/{file.Id}/documents", DocumentPayload("memo-1", "JUUL"));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.Status);

        var tooWide = await SendAsync<DocumentBody>(admin, HttpMethod.Post, $"/api/admin/cases/{file.Id}/documents", DocumentPayload("MEMO-2", "BOTH"));
        Assert.Equal(HttpStatusCode.BadRequest, tooWide.Status);

        var published = await SendAsync<DocumentBody>(admin, HttpMethod.Post, $"/api/admin/cases/{file.Id}/documents/{created.Body.Id}/status", new { status = "PUBLISHED" });
        Assert.Equal("PUBLISHED", published.Body!.Status);
        var hidden = await SendAsync<DocumentBody>(admin, HttpMethod.Post, $"/api/admin/cases/{file.Id}/documents/{created.Body.Id}/status", new { status = "DRAFT" });
        Assert.Equal("DRAFT", hidden.Body!.Status);
    }

    [Fact]
    public async Task Participants_only_read_the_published_papers_addressed_to_them()
    {
        using var admin = await _fixture.SignInAsync("administrator", CaseApiFixture.AdminPassword);
        var juulCase = await CreateCaseAsync(admin, "PRIVATE-JUUL-001", "Juul only", "JUUL");
        var amberCase = await CreateCaseAsync(admin, "PRIVATE-AMBER-001", "Amber only", "AMBER");
        var shared = await CreateCaseAsync(admin, "PRIVATE-BOTH-001", "Shared file", "BOTH");
        var draft = await CreateCaseAsync(admin, "PRIVATE-DRAFT-001", "Still closed", "BOTH");
        await SendAsync<CaseBody>(admin, HttpMethod.Post, $"/api/admin/cases/{juulCase.Id}/status", new { status = "PUBLISHED" });
        await SendAsync<CaseBody>(admin, HttpMethod.Post, $"/api/admin/cases/{amberCase.Id}/status", new { status = "PUBLISHED" });
        await SendAsync<CaseBody>(admin, HttpMethod.Post, $"/api/admin/cases/{shared.Id}/status", new { status = "PUBLISHED" });
        await SendAsync<CaseBody>(admin, HttpMethod.Post, $"/api/admin/cases/{draft.Id}/status", new { status = "ARCHIVED" });

        var sharedDocument = await SendAsync<DocumentBody>(admin, HttpMethod.Post, $"/api/admin/cases/{shared.Id}/documents", DocumentPayload("SHARED-1", "BOTH"));
        await SendAsync<DocumentBody>(admin, HttpMethod.Post, $"/api/admin/cases/{shared.Id}/documents/{sharedDocument.Body!.Id}/status", new { status = "PUBLISHED" });
        var juulDocument = await SendAsync<DocumentBody>(admin, HttpMethod.Post, $"/api/admin/cases/{shared.Id}/documents", DocumentPayload("JUUL-ONLY-1", "JUUL"));
        await SendAsync<DocumentBody>(admin, HttpMethod.Post, $"/api/admin/cases/{shared.Id}/documents/{juulDocument.Body!.Id}/status", new { status = "PUBLISHED" });
        var unpublished = await SendAsync<DocumentBody>(admin, HttpMethod.Post, $"/api/admin/cases/{shared.Id}/documents", DocumentPayload("UNSENT-1", "BOTH"));
        var amberDocument = await SendAsync<DocumentBody>(admin, HttpMethod.Post, $"/api/admin/cases/{amberCase.Id}/documents", DocumentPayload("AMBER-1", "AMBER"));
        await SendAsync<DocumentBody>(admin, HttpMethod.Post, $"/api/admin/cases/{amberCase.Id}/documents/{amberDocument.Body!.Id}/status", new { status = "PUBLISHED" });
        var draftCaseDocument = await SendAsync<DocumentBody>(admin, HttpMethod.Post, $"/api/admin/cases/{draft.Id}/documents", DocumentPayload("BURIED-1", "BOTH"));
        await SendAsync<DocumentBody>(admin, HttpMethod.Post, $"/api/admin/cases/{draft.Id}/documents/{draftCaseDocument.Body!.Id}/status", new { status = "PUBLISHED" });

        using var juul = await _fixture.SignInAsync("juul", CaseApiFixture.ClientPassword);
        Assert.Equal(HttpStatusCode.Forbidden, (await juul.GetAsync("/api/admin/cases")).StatusCode);
        var juulCases = await juul.GetFromJsonAsync<ReadingBody[]>("/api/client/cases");
        Assert.Contains(juulCases!, item => item.Id == juulCase.Id);
        Assert.Contains(juulCases!, item => item.Id == shared.Id);
        Assert.DoesNotContain(juulCases!, item => item.Id == amberCase.Id);
        Assert.DoesNotContain(juulCases!, item => item.Id == draft.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await juul.GetAsync($"/api/client/cases/{amberCase.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await juul.GetAsync($"/api/client/cases/{draft.Id}")).StatusCode);

        var juulCaseBody = await juul.GetFromJsonAsync<ReadingBody>($"/api/client/cases/{shared.Id}");
        Assert.Contains(juulCaseBody!.Documents, item => item.Id == sharedDocument.Body!.Id);
        Assert.Contains(juulCaseBody.Documents, item => item.Id == juulDocument.Body!.Id);
        Assert.DoesNotContain(juulCaseBody.Documents, item => item.Id == unpublished.Body!.Id);

        var juulPapers = await juul.GetFromJsonAsync<ListedPaper[]>("/api/client/documents");
        Assert.Contains(juulPapers!, item => item.Id == sharedDocument.Body!.Id);
        Assert.DoesNotContain(juulPapers!, item => item.Id == unpublished.Body!.Id);
        Assert.DoesNotContain(juulPapers!, item => item.Id == amberDocument.Body!.Id);
        Assert.DoesNotContain(juulPapers!, item => item.Id == draftCaseDocument.Body!.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await juul.GetAsync($"/api/client/documents/{amberDocument.Body!.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await juul.GetAsync($"/api/client/documents/{draftCaseDocument.Body!.Id}")).StatusCode);
        var juulPaper = await juul.GetFromJsonAsync<PaperBody>($"/api/client/documents/{juulDocument.Body!.Id}");
        Assert.Contains("For the named reader", juulPaper!.Content, StringComparison.Ordinal);

        using var amber = await _fixture.SignInAsync("amber", CaseApiFixture.ClientPassword);
        var amberCases = await amber.GetFromJsonAsync<ReadingBody[]>("/api/client/cases");
        Assert.Contains(amberCases!, item => item.Id == amberCase.Id);
        Assert.Contains(amberCases!, item => item.Id == shared.Id);
        Assert.DoesNotContain(amberCases!, item => item.Id == juulCase.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await amber.GetAsync($"/api/client/cases/{juulCase.Id}")).StatusCode);
        var amberShared = await amber.GetFromJsonAsync<ReadingBody>($"/api/client/cases/{shared.Id}");
        Assert.DoesNotContain(amberShared!.Documents, item => item.Id == juulDocument.Body!.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await amber.GetAsync($"/api/client/documents/{juulDocument.Body!.Id}")).StatusCode);
    }

    [Fact]
    public void A_document_cannot_outgrow_its_case()
    {
        var now = DateTimeOffset.UtcNow;
        var file = CaseFile.Open(Guid.CreateVersion7(), "HOUSE-1", "House", "Summary", null, Audiences.Juul, 0, now);
        Assert.Throws<InvalidOperationException>(() => file.AddDocument(Guid.CreateVersion7(), "NOTE-1", "Note", "Letter", null, "Text", Audiences.Both, 0, now));
        var document = file.AddDocument(Guid.CreateVersion7(), "NOTE-1", "Note", "Letter", null, "Text", Audiences.Juul, 0, now);
        file.SetStatus(CaseStatuses.Published, now);
        file.SetDocumentStatus(document.Id, DocumentStatuses.Published, now);
        Assert.True(file.DocumentIsOpenTo(document, Participants.Juul));
        Assert.False(file.DocumentIsOpenTo(document, Participants.Amber));
        file.SetStatus(CaseStatuses.Draft, now);
        Assert.False(file.DocumentIsOpenTo(document, Participants.Juul));
    }

    private static async Task<CaseBody> CreateCaseAsync(HttpClient admin, string reference, string title, string audience)
    {
        var created = await SendAsync<CaseBody>(admin, HttpMethod.Post, "/api/admin/cases", CasePayload(reference, title, audience));
        Assert.Equal(HttpStatusCode.Created, created.Status);
        return created.Body!;
    }

    private static object CasePayload(string reference, string title, string audience)
    {
        return new { reference, title, summary = "Held with the house.", introduction = "", audience };
    }

    private static object DocumentPayload(string reference, string audience)
    {
        return new { reference, title = "A note", category = "Memorandum", description = "", content = "For the named reader.", audience };
    }

    private static async Task<(HttpStatusCode Status, T? Body)> SendAsync<T>(HttpClient client, HttpMethod method, string path, object payload)
    {
        var token = await client.GetFromJsonAsync<CsrfResponse>("/api/auth/csrf");
        var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(payload) };
        request.Headers.TryAddWithoutValidation("Origin", "http://localhost:5173");
        request.Headers.TryAddWithoutValidation("X-CSRF-TOKEN", token?.Token);
        var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return (response.StatusCode, default);
        }

        var body = await response.Content.ReadFromJsonAsync<T>();
        return (response.StatusCode, body);
    }

    private sealed record CsrfResponse(string? Token);

    private sealed record CaseBody(Guid Id, string Reference, string Title, string Status);

    private sealed record DocumentBody(Guid Id, string Reference, string Status);

    private sealed record ReadingBody(Guid Id, string Reference, ReadingDoc[] Documents);

    private sealed record ReadingDoc(Guid Id, string Reference);

    private sealed record PaperBody(Guid Id, string Content);

    private sealed record ListedPaper(Guid Id);
}

public sealed class CaseApiFixture : IDisposable
{
    public const string ClientPassword = "client-password-1894";
    public const string AdminPassword = "admin-password-1894";

    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly WebApplicationFactory<Program> _factory;

    public CaseApiFixture()
    {
        _connection.Open();
        var identities = new InMemoryIdentityStore();
        var hasher = new IdentityPasswordHasher();
        var now = DateTimeOffset.UtcNow;
        identities.Add(User.Create(Guid.CreateVersion7(), "Juul-V", "Juul", hasher.Hash(ClientPassword), SecurityStamp.Create(), now, [new Role(RoleNames.ClientId, RoleNames.Client)]));
        identities.Add(User.Create(Guid.CreateVersion7(), "Amber-D", "Amber", hasher.Hash(ClientPassword), SecurityStamp.Create(), now, [new Role(RoleNames.ClientId, RoleNames.Client)]));
        identities.Add(User.Create(Guid.CreateVersion7(), "administrator", "Administrator", hasher.Hash(AdminPassword), SecurityStamp.Create(), now, [new Role(RoleNames.AdminId, RoleNames.Admin)]));

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Auth:AllowedOrigins:0", "http://localhost:5173");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IIdentityStore>();
                services.AddSingleton<IIdentityStore>(identities);
                services.RemoveAll<DbContextOptions<WeddingBankDbContext>>();
                foreach (var descriptor in services.Where(item => item.ServiceType == typeof(IDbContextOptionsConfiguration<WeddingBankDbContext>)).ToList())
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<WeddingBankDbContext>(options => options.UseSqlite(_connection));
            });
        });

        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<WeddingBankDbContext>().Database.EnsureCreated();
    }

    public async Task<HttpClient> SignInAsync(string login, string password)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true, AllowAutoRedirect = false });
        var token = await client.GetFromJsonAsync<TokenBody>("/api/auth/csrf");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { login, password }),
        };
        request.Headers.TryAddWithoutValidation("Origin", "http://localhost:5173");
        request.Headers.TryAddWithoutValidation("X-CSRF-TOKEN", token?.Token);
        var response = await client.SendAsync(request);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"Sign-in for {login} returned {(int)response.StatusCode}.");
        }

        return client;
    }

    public void Dispose()
    {
        _factory.Dispose();
        _connection.Dispose();
    }

    private sealed record TokenBody(string? Token);

    private sealed class InMemoryIdentityStore : IIdentityStore
    {
        private readonly List<User> _users = [];
        private readonly List<AuthSession> _sessions = [];

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

        public void AddAuditEvent(AuditEvent auditEvent)
        {
        }

        public Task<IReadOnlyList<AuditEntry>> ListRecentAuditAsync(int limit, CancellationToken cancellationToken)
        {
            IReadOnlyList<AuditEntry> entries = [];
            return Task.FromResult(entries);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
