namespace WeddingBank.Application.Game;

public interface IDocumentReadStore
{
    Task<bool> IsEstablishedAsync(CancellationToken cancellationToken);

    Task<IReadOnlySet<Guid>> ReadDocumentIdsAsync(string login, CancellationToken cancellationToken);

    Task AcknowledgeAsync(string login, IReadOnlyCollection<Guid> documentIds, DateTimeOffset readAt, CancellationToken cancellationToken);

    Task MarkReadAsync(string login, Guid documentId, DateTimeOffset readAt, CancellationToken cancellationToken);

    Task EstablishAsync(DateTimeOffset establishedAt, CancellationToken cancellationToken);
}
