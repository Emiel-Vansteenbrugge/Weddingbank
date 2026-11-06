using WeddingBank.Domain.Game;

namespace WeddingBank.Application.Game;

public interface ICaseStore
{
    Task<IReadOnlyList<CaseFile>> ListAsync(CancellationToken cancellationToken);

    Task<CaseFile?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ReferenceInUseAsync(string reference, Guid? exceptId, CancellationToken cancellationToken);

    void Add(CaseFile file);

    Task SaveAsync(CancellationToken cancellationToken);
}
