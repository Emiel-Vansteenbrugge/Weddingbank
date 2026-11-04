using WeddingBank.Application.Clients;
using WeddingBank.Domain.Clients;

namespace WeddingBank.Tests.Clients;

sealed class MemoryClientStore : IClientStore
{
    private readonly List<Client> _clients = [];
    private readonly List<ClientUser> _links = [];
    private readonly Dictionary<string, AccountProfile> _accounts = new(StringComparer.OrdinalIgnoreCase);

    public void AddAccount(AccountProfile account) => _accounts[account.Login] = account;

    public Task<bool> ReferenceInUseAsync(string reference, CancellationToken cancellationToken) =>
        Task.FromResult(_clients.Any(client => client.ClientReference == reference));

    public Task<Client?> FindClientAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_clients.SingleOrDefault(client => client.Id == id));

    public Task<ClientListSlice> ListClientsAsync(ClientListQuery query, CancellationToken cancellationToken)
    {
        var filtered = _clients.Where(client =>
            (query.Status is null || client.Status == query.Status)
            && (query.Search is null || Matches(client, query.Search))).ToList();
        var pageSize = query.PageSize is < 1 or > 50 ? 20 : query.PageSize;
        var page = query.Page < 1 ? 1 : query.Page;
        IReadOnlyList<Client> items = filtered
            .OrderByDescending(client => client.CreatedAt)
            .ThenBy(client => client.ClientReference)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        return Task.FromResult(new ClientListSlice(
            _clients.Count,
            _clients.Count(client => client.Status == ClientStatus.Active),
            _clients.Count(client => client.Status == ClientStatus.Inactive),
            _clients.Count(client => client.Status == ClientStatus.Suspended),
            filtered.Count,
            items));
    }

    public Task<IReadOnlyList<Client>> ListClientsForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var ids = _links.Where(link => link.UserId == userId).Select(link => link.ClientId).ToHashSet();
        IReadOnlyList<Client> records = _clients
            .Where(client => ids.Contains(client.Id))
            .OrderBy(client => client.LegalName)
            .ThenBy(client => client.ClientReference)
            .ToList();
        return Task.FromResult(records);
    }

    public Task<bool> LinkExistsAsync(Guid clientId, Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(_links.Any(link => link.ClientId == clientId && link.UserId == userId));

    public Task<ClientUser?> FindLinkAsync(Guid clientId, Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(_links.SingleOrDefault(link => link.ClientId == clientId && link.UserId == userId));

    public Task<AccountProfile?> FindAccountByLoginAsync(string login, CancellationToken cancellationToken) =>
        Task.FromResult(_accounts.TryGetValue(login, out var account) ? account : null);

    public Task<AccountProfile?> FindAccountByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_accounts.Values.SingleOrDefault(account => account.Id == id));

    public Task<IReadOnlyList<LinkedAccount>> ListLinkedAccountsAsync(Guid clientId, CancellationToken cancellationToken)
    {
        IReadOnlyList<LinkedAccount> rows = _links
            .Where(link => link.ClientId == clientId)
            .Select(link =>
            {
                var account = _accounts.Values.Single(account => account.Id == link.UserId);
                return new LinkedAccount(account.Id, account.Login, account.DisplayName, account.Roles, link.LinkedAt);
            })
            .OrderBy(row => row.Login)
            .ToArray();
        return Task.FromResult(rows);
    }

    public void AddClient(Client client) => _clients.Add(client);

    public void AddLink(ClientUser link) => _links.Add(link);

    public void RemoveLink(ClientUser link) => _links.Remove(link);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        if (_clients.GroupBy(client => client.ClientReference).Any(group => group.Count() > 1))
        {
            throw new ClientWriteConflictException(ClientConflictKind.Reference);
        }

        if (_links.GroupBy(link => (link.ClientId, link.UserId)).Any(group => group.Count() > 1))
        {
            throw new ClientWriteConflictException(ClientConflictKind.Link);
        }

        return Task.CompletedTask;
    }

    private static bool Matches(Client client, string search)
    {
        var term = search.ToLowerInvariant();
        return client.ClientReference.ToLowerInvariant().Contains(term)
            || client.LegalName.ToLowerInvariant().Contains(term)
            || (client.DisplayName?.ToLowerInvariant().Contains(term) ?? false)
            || client.EmailAddress.ToLowerInvariant().Contains(term);
    }
}

sealed class FixedReferences(params string[] values) : IClientReferenceSource
{
    private int _index;

    public string Create() => values[Math.Min(_index++, values.Length - 1)];
}

sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
