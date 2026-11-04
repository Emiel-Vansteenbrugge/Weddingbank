using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WeddingBank.Application.Clients;
using WeddingBank.Application.Identity;
using WeddingBank.Domain.Clients;
using WeddingBank.Domain.Identity;
using WeddingBank.Infrastructure.Clients;
using WeddingBank.Infrastructure.Identity;
using WeddingBank.Infrastructure.Persistence;

namespace WeddingBank.Tests.Clients;

public sealed class ClientRelationalTests
{
    [Fact]
    public async Task The_client_tables_keep_references_unique_and_do_not_cascade_a_removed_link()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<WeddingBankDbContext>().UseSqlite(connection).Options;
        var now = new DateTimeOffset(2026, 11, 4, 9, 0, 0, TimeSpan.Zero);
        Guid clientId;
        Guid userId;

        await using (var db = new WeddingBankDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
            var role = await db.Roles.SingleAsync(item => item.Name == RoleNames.Client);
            var user = User.Create(
                Guid.CreateVersion7(),
                "juul",
                "Juul",
                new IdentityPasswordHasher().Hash("client-password-1894"),
                SecurityStamp.Create(),
                now,
                [role]);
            db.Users.Add(user);
            await db.SaveChangesAsync();
            userId = user.Id;

            var clients = new EfClientStore(db);
            var service = new ClientService(clients, new EfIdentityStore(db), new FixedReferences("AJ-ABCD-EFGH", "AJ-WXYZ-2345"), new FixedClock(now));
            var created = await service.CreateAsync(Sample("secret@example.com"), userId, null, CancellationToken.None);
            var other = await service.CreateAsync(Sample("nord@example.com") with { LegalName = "Atelier Nord" }, userId, null, CancellationToken.None);
            Assert.Equal(OperationStatus.Ok, created.Status);
            clientId = created.Value!.Id;

            var revised = await service.UpdateAsync(
                clientId,
                new ClientRevision { PhoneNumber = "+33 1 42 00 00 00", PhoneNumberSet = true },
                userId,
                null,
                CancellationToken.None);
            Assert.Equal("Maison Example", revised.Value!.LegalName);
            Assert.Equal("+33 1 42 00 00 00", revised.Value.PhoneNumber);

            var linked = await service.LinkAsync(clientId, "juul", userId, null, CancellationToken.None);
            var duplicate = await service.LinkAsync(clientId, "juul", userId, null, CancellationToken.None);
            Assert.Equal(OperationStatus.Ok, linked.Status);
            Assert.Equal(OperationStatus.Conflict, duplicate.Status);
            Assert.DoesNotContain("PasswordHash", linked.Value!.ToString(), StringComparison.Ordinal);

            var own = await service.ListOwnAsync(userId, CancellationToken.None);
            Assert.Contains(own, relationship => relationship.EmailAddress == "secret@example.com");

            var suspended = await service.ChangeStatusAsync(clientId, "SUSPENDED", userId, null, CancellationToken.None);
            Assert.Equal("SUSPENDED", suspended.Value!.Status);
            var closed = await service.GetOwnAsync(userId, clientId, CancellationToken.None);
            Assert.NotNull(closed);
            Assert.False(closed.Active);
            Assert.Null(closed.EmailAddress);
            Assert.Null(await service.GetOwnAsync(userId, other.Value!.Id, CancellationToken.None));

            var refused = await service.ChangeStatusAsync(other.Value.Id, "SUSPENDED", userId, null, CancellationToken.None);
            await service.ChangeStatusAsync(other.Value.Id, "INACTIVE", userId, null, CancellationToken.None);
            refused = await service.ChangeStatusAsync(other.Value.Id, "SUSPENDED", userId, null, CancellationToken.None);
            Assert.Equal(OperationStatus.Invalid, refused.Status);

            var removed = await service.UnlinkAsync(clientId, userId, userId, null, CancellationToken.None);
            Assert.Equal(OperationStatus.Ok, removed.Status);
            Assert.Empty(await service.ListOwnAsync(userId, CancellationToken.None));
            Assert.NotNull(await db.Users.SingleOrDefaultAsync(item => item.Id == userId));
            Assert.NotNull(await db.Clients.SingleOrDefaultAsync(item => item.Id == clientId));

            Assert.Equal(1, await db.Clients.CountAsync(client => client.EmailAddress == "nord@example.com"));
            var wildcard = await db.Clients.CountAsync(client =>
                client.ClientReference.ToLower().Contains("%")
                || client.LegalName.ToLower().Contains("%")
                || (client.DisplayName != null && client.DisplayName.ToLower().Contains("%"))
                || client.EmailAddress.ToLower().Contains("%"));
            Assert.Equal(0, wildcard);

            clients.AddClient(Client.Open(
                Guid.CreateVersion7(),
                "AJ-ABCD-EFGH",
                "Duplicate",
                null,
                "duplicate@example.com",
                null,
                null,
                null,
                null,
                null,
                null,
                ClientStatus.Active,
                now));
            await Assert.ThrowsAsync<ClientWriteConflictException>(() => clients.SaveChangesAsync(CancellationToken.None));

            var events = await db.AuditEvents.Select(audit => audit.EventType).ToListAsync();
            Assert.Contains(AuditEvent.ClientCreated, events);
            Assert.Contains(AuditEvent.ClientUpdated, events);
            Assert.Contains(AuditEvent.ClientStatusChanged, events);
            Assert.Contains(AuditEvent.ClientUserLinked, events);
            Assert.Contains(AuditEvent.ClientUserUnlinked, events);
        }

        await using var blocked = new WeddingBankDbContext(options);
        var again = await blocked.ClientUsers.SingleOrDefaultAsync();
        Assert.Null(again);
        blocked.ClientUsers.Add(ClientUser.Link(clientId, userId, now));
        await blocked.SaveChangesAsync();
        blocked.ChangeTracker.Clear();
        var linkedUser = await blocked.Users.SingleAsync(item => item.Id == userId);
        blocked.Users.Remove(linkedUser);
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => blocked.SaveChangesAsync());
        blocked.ChangeTracker.Clear();
        Assert.NotNull(await blocked.Users.SingleOrDefaultAsync(item => item.Id == userId));
        Assert.NotNull(await blocked.Clients.SingleOrDefaultAsync(item => item.Id == clientId));
    }

    private static CreateClient Sample(string email) => new(
        "Maison Example",
        "Example",
        email,
        null,
        "1 Rue de la Paix",
        null,
        "75002",
        "Paris",
        "France",
        "ACTIVE");
}
