using WeddingBank.Domain.Clients;

namespace WeddingBank.Application.Clients;

public interface IClientStore
{
    Task<bool> ReferenceInUseAsync(string reference, CancellationToken cancellationToken);

    Task<Client?> FindClientAsync(Guid id, CancellationToken cancellationToken);

    Task<ClientListSlice> ListClientsAsync(ClientListQuery query, CancellationToken cancellationToken);

    Task<IReadOnlyList<Client>> ListClientsForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> LinkExistsAsync(Guid clientId, Guid userId, CancellationToken cancellationToken);

    Task<ClientUser?> FindLinkAsync(Guid clientId, Guid userId, CancellationToken cancellationToken);

    Task<AccountProfile?> FindAccountByLoginAsync(string login, CancellationToken cancellationToken);

    Task<AccountProfile?> FindAccountByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<LinkedAccount>> ListLinkedAccountsAsync(Guid clientId, CancellationToken cancellationToken);

    void AddClient(Client client);

    void AddLink(ClientUser link);

    void RemoveLink(ClientUser link);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
