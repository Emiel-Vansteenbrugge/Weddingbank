using Microsoft.EntityFrameworkCore;
using WeddingBank.Application.Game;
using WeddingBank.Domain.Game;
using WeddingBank.Infrastructure.Persistence;

namespace WeddingBank.Infrastructure.Game;

public sealed class EfDocumentReadStore(WeddingBankDbContext db) : IDocumentReadStore
{
    public Task<bool> IsEstablishedAsync(CancellationToken cancellationToken)
    {
        return db.DocumentReadBaselines.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> ReadDocumentIdsAsync(string login, CancellationToken cancellationToken)
    {
        var ids = await db.DocumentReadings
            .AsNoTracking()
            .Where(item => item.ParticipantLogin == login)
            .Select(item => item.DocumentId)
            .ToListAsync(cancellationToken);
        return ids.ToHashSet();
    }

    public async Task AcknowledgeAsync(string login, IReadOnlyCollection<Guid> documentIds, DateTimeOffset readAt, CancellationToken cancellationToken)
    {
        if (documentIds.Count == 0)
        {
            return;
        }

        var existing = await db.DocumentReadings
            .Where(item => item.ParticipantLogin == login && documentIds.Contains(item.DocumentId))
            .Select(item => item.DocumentId)
            .ToListAsync(cancellationToken);
        var known = existing.ToHashSet();
        foreach (var documentId in documentIds)
        {
            if (known.Add(documentId))
            {
                db.DocumentReadings.Add(DocumentReading.Open(Guid.CreateVersion7(), documentId, login, readAt));
            }
        }

        await SaveIgnoringDuplicateAsync(cancellationToken);
    }

    public async Task MarkReadAsync(string login, Guid documentId, DateTimeOffset readAt, CancellationToken cancellationToken)
    {
        var read = await db.DocumentReadings.AnyAsync(
            item => item.DocumentId == documentId && item.ParticipantLogin == login,
            cancellationToken);
        if (read)
        {
            return;
        }

        db.DocumentReadings.Add(DocumentReading.Open(Guid.CreateVersion7(), documentId, login, readAt));
        await SaveIgnoringDuplicateAsync(cancellationToken);
    }

    public async Task EstablishAsync(DateTimeOffset establishedAt, CancellationToken cancellationToken)
    {
        if (await db.DocumentReadBaselines.AnyAsync(cancellationToken))
        {
            return;
        }

        db.DocumentReadBaselines.Add(DocumentReadBaseline.Establish(establishedAt));
        await SaveIgnoringDuplicateAsync(cancellationToken);
    }

    private async Task SaveIgnoringDuplicateAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
        }
    }
}
