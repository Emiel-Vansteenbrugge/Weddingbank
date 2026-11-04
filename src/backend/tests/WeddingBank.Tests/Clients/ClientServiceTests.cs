using WeddingBank.Application.Clients;
using WeddingBank.Application.Identity;
using WeddingBank.Domain.Clients;
using WeddingBank.Domain.Identity;

namespace WeddingBank.Tests.Clients;

public sealed class ClientServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 4, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Actor = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public void A_reference_uses_the_house_alphabet_and_inactive_cannot_become_suspended()
    {
        Assert.True(ClientReferenceCode.IsCanonical("AJ-ABCD-EFGH"));
        Assert.False(ClientReferenceCode.IsCanonical("AJ-0000-1111"));
        Assert.False(ClientStatuses.CanMove(ClientStatus.Inactive, ClientStatus.Suspended));
        Assert.True(ClientStatuses.CanMove(ClientStatus.Active, ClientStatus.Suspended));
        Assert.False(ClientStatuses.CanMove(ClientStatus.Active, ClientStatus.Active));
    }

    [Fact]
    public async Task Opening_a_client_assigns_a_reference_and_records_the_action()
    {
        var store = new MemoryClientStore();
        var audits = new AuditSink();
        var service = Service(store, audits, "AJ-ABCD-EFGH");

        var result = await service.CreateAsync(Sample(), Actor, null, CancellationToken.None);

        Assert.Equal(OperationStatus.Ok, result.Status);
        Assert.Equal("AJ-ABCD-EFGH", result.Value!.ClientReference);
        Assert.Equal("ACTIVE", result.Value.Status);
        Assert.Equal(AuditEvent.ClientCreated, audits.Events[0].EventType);
        Assert.Equal(Actor, audits.Events[0].ActorUserId);
        Assert.Equal(result.Value.Id.ToString(), audits.Events[0].TargetId);
        Assert.DoesNotContain("secret@example.com", audits.Events[0].MetadataJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_missing_legal_name_is_rejected_before_a_record_is_opened()
    {
        var store = new MemoryClientStore();
        var audits = new AuditSink();
        var service = Service(store, audits, "AJ-ABCD-EFGH");

        var result = await service.CreateAsync(Sample() with { LegalName = "  " }, Actor, null, CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.Contains("legalName", result.Errors!.Keys);
        Assert.Equal(0, (await store.ListClientsAsync(new ClientListQuery(null, null, 1, 20), CancellationToken.None)).Total);
        Assert.Empty(audits.Events);
    }

    [Fact]
    public async Task An_invalid_email_is_rejected()
    {
        var service = Service(new MemoryClientStore(), new AuditSink(), "AJ-ABCD-EFGH");

        var result = await service.CreateAsync(Sample() with { EmailAddress = "not-an-email" }, Actor, null, CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.Contains("emailAddress", result.Errors!.Keys);
    }

    [Fact]
    public async Task A_taken_reference_is_replaced_before_the_record_is_opened()
    {
        var store = new MemoryClientStore();
        var first = await Service(store, new AuditSink(), "AJ-ABCD-EFGH").CreateAsync(Sample(), Actor, null, CancellationToken.None);
        var second = await Service(store, new AuditSink(), "AJ-ABCD-EFGH", "AJ-WXYZ-2345")
            .CreateAsync(Sample() with { EmailAddress = "other@example.com" }, Actor, null, CancellationToken.None);

        Assert.Equal(OperationStatus.Ok, first.Status);
        Assert.Equal("AJ-WXYZ-2345", second.Value!.ClientReference);
    }

    [Fact]
    public async Task A_revision_changes_only_the_field_that_was_submitted()
    {
        var store = new MemoryClientStore();
        var created = await Service(store, new AuditSink(), "AJ-ABCD-EFGH").CreateAsync(Sample(), Actor, null, CancellationToken.None);
        var revision = new ClientRevision { PhoneNumber = "+33 1 42 00 00 00", PhoneNumberSet = true };

        var result = await Service(store, new AuditSink(), "AJ-ABCD-EFGH").UpdateAsync(
            created.Value!.Id, revision, Actor, null, CancellationToken.None);

        Assert.Equal(OperationStatus.Ok, result.Status);
        Assert.Equal("+33 1 42 00 00 00", result.Value!.PhoneNumber);
        Assert.Equal("Maison Example", result.Value.LegalName);
        Assert.Equal("secret@example.com", result.Value.EmailAddress);
    }

    [Fact]
    public async Task A_status_can_move_from_active_to_suspended_and_not_from_inactive_to_suspended()
    {
        var store = new MemoryClientStore();
        var service = Service(store, new AuditSink(), "AJ-ABCD-EFGH", "AJ-WXYZ-2345");
        var active = await service.CreateAsync(Sample(), Actor, null, CancellationToken.None);
        var inactive = await service.CreateAsync(
            Sample() with { EmailAddress = "other@example.com", Status = "INACTIVE" }, Actor, null, CancellationToken.None);

        var suspended = await service.ChangeStatusAsync(active.Value!.Id, "SUSPENDED", Actor, null, CancellationToken.None);
        var refused = await service.ChangeStatusAsync(inactive.Value!.Id, "SUSPENDED", Actor, null, CancellationToken.None);

        Assert.Equal("SUSPENDED", suspended.Value!.Status);
        Assert.Equal(OperationStatus.Invalid, refused.Status);
        Assert.Equal(ClientStatus.Inactive, (await store.FindClientAsync(inactive.Value.Id, CancellationToken.None))!.Status);
    }

    [Fact]
    public async Task The_same_status_is_not_written_again()
    {
        var store = new MemoryClientStore();
        var created = await Service(store, new AuditSink(), "AJ-ABCD-EFGH").CreateAsync(Sample(), Actor, null, CancellationToken.None);

        var result = await Service(store, new AuditSink(), "AJ-ABCD-EFGH").ChangeStatusAsync(
            created.Value!.Id, "ACTIVE", Actor, null, CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task An_account_can_be_linked_once_and_the_link_can_be_removed()
    {
        var store = new MemoryClientStore();
        var userId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        store.AddAccount(new AccountProfile(userId, "juul", "Juul", ["CLIENT"]));
        var created = await Service(store, new AuditSink(), "AJ-ABCD-EFGH").CreateAsync(Sample(), Actor, null, CancellationToken.None);
        var service = Service(store, new AuditSink(), "AJ-ABCD-EFGH");

        var linked = await service.LinkAsync(created.Value!.Id, "juul", Actor, null, CancellationToken.None);
        var duplicate = await service.LinkAsync(created.Value.Id, "juul", Actor, null, CancellationToken.None);
        var removed = await service.UnlinkAsync(created.Value.Id, userId, Actor, null, CancellationToken.None);

        Assert.Equal(OperationStatus.Ok, linked.Status);
        Assert.Equal(OperationStatus.Conflict, duplicate.Status);
        Assert.Equal(OperationStatus.Ok, removed.Status);
        Assert.Empty(await store.ListLinkedAccountsAsync(created.Value.Id, CancellationToken.None));
        Assert.NotNull(await store.FindClientAsync(created.Value.Id, CancellationToken.None));
        Assert.NotNull(await store.FindAccountByLoginAsync("juul", CancellationToken.None));
    }

    [Fact]
    public async Task A_suspended_relationship_does_not_carry_contact_details_to_the_client()
    {
        var store = new MemoryClientStore();
        var userId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        store.AddAccount(new AccountProfile(userId, "juul", "Juul", ["CLIENT"]));
        var service = Service(store, new AuditSink(), "AJ-ABCD-EFGH");
        var created = await service.CreateAsync(Sample(), Actor, null, CancellationToken.None);
        await service.LinkAsync(created.Value!.Id, "juul", Actor, null, CancellationToken.None);
        await service.ChangeStatusAsync(created.Value.Id, "SUSPENDED", Actor, null, CancellationToken.None);

        var own = await service.ListOwnAsync(userId, CancellationToken.None);
        var hidden = await service.GetOwnAsync(userId, Guid.NewGuid(), CancellationToken.None);

        Assert.Single(own);
        Assert.False(own[0].Active);
        Assert.Null(own[0].EmailAddress);
        Assert.Null(hidden);
    }

    [Fact]
    public async Task A_user_without_a_relationship_receives_an_empty_list()
    {
        var service = Service(new MemoryClientStore(), new AuditSink(), "AJ-ABCD-EFGH");

        var own = await service.ListOwnAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Empty(own);
    }

    [Fact]
    public async Task Search_matches_a_reference_name_or_email_and_keeps_the_page_small()
    {
        var store = new MemoryClientStore();
        var service = Service(store, new AuditSink(), "AJ-ABCD-EFGH", "AJ-WXYZ-2345");
        await service.CreateAsync(Sample(), Actor, null, CancellationToken.None);
        await service.CreateAsync(Sample() with { LegalName = "Atelier Nord", EmailAddress = "nord@example.com" }, Actor, null, CancellationToken.None);

        var byEmail = await service.ListAsync(new ClientListQuery("nord@example.com", null, 1, 20), CancellationToken.None);
        var byStatus = await service.ListAsync(new ClientListQuery(null, ClientStatus.Inactive, 1, 20), CancellationToken.None);
        var page = await service.ListAsync(new ClientListQuery(null, null, 2, 1), CancellationToken.None);

        Assert.Equal(1, byEmail.FilteredCount);
        Assert.Equal("Atelier Nord", byEmail.Items[0].LegalName);
        Assert.Equal(0, byStatus.FilteredCount);
        Assert.Equal(2, page.FilteredCount);
        Assert.Single(page.Items);
    }

    private static ClientService Service(MemoryClientStore store, AuditSink audits, params string[] references) =>
        new(store, audits, new FixedReferences(references), new FixedClock(Now));

    private static CreateClient Sample() => new(
        "Maison Example",
        "Example",
        "secret@example.com",
        null,
        "1 Rue de la Paix",
        null,
        "75002",
        "Paris",
        "France",
        "ACTIVE");

    private sealed class AuditSink : IIdentityStore
    {
        public List<AuditEvent> Events { get; } = [];

        public void AddAuditEvent(AuditEvent auditEvent) => Events.Add(auditEvent);

        public Task<User?> FindUserByLoginAsync(string login, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<User?> FindUserByIdAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<AuthSession?> FindSessionAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();

        public void AddSession(AuthSession session) => throw new NotSupportedException();

        public Task<IReadOnlyList<AuditEntry>> ListRecentAuditAsync(int limit, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
