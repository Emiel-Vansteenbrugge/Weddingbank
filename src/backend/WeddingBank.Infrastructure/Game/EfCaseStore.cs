using Microsoft.EntityFrameworkCore;
using WeddingBank.Application.Game;
using WeddingBank.Domain.Game;
using WeddingBank.Infrastructure.Persistence;

namespace WeddingBank.Infrastructure.Game;

public sealed class EfCaseStore(WeddingBankDbContext db) : ICaseStore
{
    public async Task<IReadOnlyList<CaseFile>> ListAsync(CancellationToken cancellationToken)
    {
        return await db.CaseFiles.Include(file => file.Documents).ToListAsync(cancellationToken);
    }

    public Task<CaseFile?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        return db.CaseFiles.Include(file => file.Documents).SingleOrDefaultAsync(file => file.Id == id, cancellationToken);
    }

    public Task<bool> ReferenceInUseAsync(string reference, Guid? exceptId, CancellationToken cancellationToken)
    {
        return exceptId is Guid id
            ? db.CaseFiles.AnyAsync(file => file.Reference == reference && file.Id != id, cancellationToken)
            : db.CaseFiles.AnyAsync(file => file.Reference == reference, cancellationToken);
    }

    public void Add(CaseFile file)
    {
        db.CaseFiles.Add(file);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return db.SaveChangesAsync(cancellationToken);
    }
}
