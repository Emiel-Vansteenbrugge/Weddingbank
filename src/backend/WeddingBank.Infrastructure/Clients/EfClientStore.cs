using Microsoft.EntityFrameworkCore;
using Npgsql;
using WeddingBank.Application.Clients;
using WeddingBank.Domain.Clients;
using WeddingBank.Infrastructure.Persistence;
using Client = WeddingBank.Domain.Clients.Client;

namespace WeddingBank.Infrastructure.Clients;

public sealed class EfClientStore(WeddingBankDbContext db) : IClientStore
{
    public Task<bool> ReferenceInUseAsync(string reference, CancellationToken cancellationToken)
    {
        return db.Clients.AnyAsync(client => client.ClientReference == reference, cancellationToken);
    }

    public Task<Client?> FindClientAsync(Guid id, CancellationToken cancellationToken)
    {
        return db.Clients.SingleOrDefaultAsync(client => client.Id == id, cancellationToken);
    }

    public async Task<ClientListSlice> ListClientsAsync(ClientListQuery query, CancellationToken cancellationToken)
    {
        var counts = await db.Clients.AsNoTracking()
            .GroupBy(client => client.Status)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var total = counts.Sum(group => group.Count);
        var active = counts.Where(group => group.Key == ClientStatus.Active).Select(group => group.Count).FirstOrDefault();
        var inactive = counts.Where(group => group.Key == ClientStatus.Inactive).Select(group => group.Count).FirstOrDefault();
        var suspended = counts.Where(group => group.Key == ClientStatus.Suspended).Select(group => group.Count).FirstOrDefault();

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 50 ? 20 : query.PageSize;
        IQueryable<Client> filtered = db.Clients.AsNoTracking();
        if (query.Status is ClientStatus status)
        {
            filtered = filtered.Where(client => client.Status == status);
        }

        var term = query.Search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            var lowered = term.ToLowerInvariant();
            filtered = filtered.Where(client =>
                client.ClientReference.ToLower().Contains(lowered)
                || client.LegalName.ToLower().Contains(lowered)
                || (client.DisplayName != null && client.DisplayName.ToLower().Contains(lowered))
                || client.EmailAddress.ToLower().Contains(lowered));
        }

        var filteredCount = await filtered.CountAsync(cancellationToken);
        var items = await filtered
            .OrderByDescending(client => client.CreatedAt)
            .ThenBy(client => client.ClientReference)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new ClientListSlice(total, active, inactive, suspended, filteredCount, items);
    }

    public async Task<IReadOnlyList<Client>> ListClientsForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await (
                from link in db.ClientUsers.AsNoTracking()
                join client in db.Clients.AsNoTracking() on link.ClientId equals client.Id
                where link.UserId == userId
                orderby client.LegalName, client.ClientReference
                select client)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> LinkExistsAsync(Guid clientId, Guid userId, CancellationToken cancellationToken)
    {
        return db.ClientUsers.AnyAsync(link => link.ClientId == clientId && link.UserId == userId, cancellationToken);
    }

    public Task<ClientUser?> FindLinkAsync(Guid clientId, Guid userId, CancellationToken cancellationToken)
    {
        return db.ClientUsers.SingleOrDefaultAsync(
            link => link.ClientId == clientId && link.UserId == userId,
            cancellationToken);
    }

    public async Task<AccountProfile?> FindAccountByLoginAsync(string login, CancellationToken cancellationToken)
    {
        var account = await db.Users.AsNoTracking()
            .Where(user => user.Login == login)
            .Select(user => new { user.Id, user.Login, user.DisplayName })
            .SingleOrDefaultAsync(cancellationToken);
        if (account is null)
        {
            return null;
        }

        return new AccountProfile(account.Id, account.Login, account.DisplayName, await RolesAsync(account.Id, cancellationToken));
    }

    public async Task<AccountProfile?> FindAccountByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await db.Users.AsNoTracking()
            .Where(user => user.Id == id)
            .Select(user => new { user.Id, user.Login, user.DisplayName })
            .SingleOrDefaultAsync(cancellationToken);
        if (account is null)
        {
            return null;
        }

        return new AccountProfile(account.Id, account.Login, account.DisplayName, await RolesAsync(account.Id, cancellationToken));
    }

    public async Task<IReadOnlyList<LinkedAccount>> ListLinkedAccountsAsync(Guid clientId, CancellationToken cancellationToken)
    {
        var rows = await (
                from link in db.ClientUsers.AsNoTracking()
                join user in db.Users.AsNoTracking() on link.UserId equals user.Id
                where link.ClientId == clientId
                orderby user.Login
                select new { user.Id, user.Login, user.DisplayName, link.LinkedAt })
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return [];
        }

        var ids = rows.Select(row => row.Id).ToArray();
        var roles = await (
                from link in db.UserRoles.AsNoTracking()
                join role in db.Roles.AsNoTracking() on link.RoleId equals role.Id
                where ids.Contains(link.UserId)
                select new { link.UserId, role.Name })
            .ToListAsync(cancellationToken);
        return rows
            .Select(row => new LinkedAccount(
                row.Id,
                row.Login,
                row.DisplayName,
                roles.Where(role => role.UserId == row.Id).Select(role => role.Name).ToArray(),
                row.LinkedAt))
            .ToArray();
    }

    public void AddClient(Client client)
    {
        db.Clients.Add(client);
    }

    public void AddLink(ClientUser link)
    {
        db.ClientUsers.Add(link);
    }

    public void RemoveLink(ClientUser link)
    {
        db.ClientUsers.Remove(link);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            throw MapConflict(exception);
        }
    }

    private async Task<IReadOnlyList<string>> RolesAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await (
                from link in db.UserRoles.AsNoTracking()
                join role in db.Roles.AsNoTracking() on link.RoleId equals role.Id
                where link.UserId == userId
                orderby role.Name
                select role.Name)
            .ToListAsync(cancellationToken);
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        if (exception.InnerException is PostgresException postgres)
        {
            return postgres.SqlState == PostgresErrorCodes.UniqueViolation;
        }

        return (exception.InnerException?.Message ?? "").Contains("UNIQUE", StringComparison.OrdinalIgnoreCase);
    }

    private static ClientWriteConflictException MapConflict(DbUpdateException exception)
    {
        var name = exception.InnerException switch
        {
            PostgresException postgres => postgres.ConstraintName ?? "",
            _ => exception.InnerException?.Message ?? "",
        };
        var kind = name.Contains("ClientUser", StringComparison.OrdinalIgnoreCase)
            ? ClientConflictKind.Link
            : ClientConflictKind.Reference;
        return new ClientWriteConflictException(kind);
    }
}
