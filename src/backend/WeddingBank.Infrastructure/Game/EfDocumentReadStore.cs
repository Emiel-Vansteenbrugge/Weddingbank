using Microsoft.EntityFrameworkCore;
using Npgsql;
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

    public Task AcknowledgeAsync(string login, IReadOnlyCollection<Guid> documentIds, DateTimeOffset readAt, CancellationToken cancellationToken)
    {
        return InsertMissingAsync(login, documentIds, readAt, cancellationToken);
    }

    public Task MarkReadAsync(string login, Guid documentId, DateTimeOffset readAt, CancellationToken cancellationToken)
    {
        return InsertMissingAsync(login, [documentId], readAt, cancellationToken);
    }

    public async Task EstablishAsync(DateTimeOffset establishedAt, CancellationToken cancellationToken)
    {
        if (await db.DocumentReadBaselines.AnyAsync(cancellationToken))
        {
            return;
        }

        var baseline = DocumentReadBaseline.Establish(establishedAt);
        db.DocumentReadBaselines.Add(baseline);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (DocumentReadingConflict.IsDuplicateBaseline(exception))
        {
            Detach(baseline);
        }
        catch (DbUpdateException)
        {
            Detach(baseline);
            throw;
        }
    }

    internal Task InsertReadingAsync(string login, Guid documentId, DateTimeOffset readAt, CancellationToken cancellationToken)
    {
        var reading = DocumentReading.Open(Guid.CreateVersion7(), documentId, login, readAt);
        db.DocumentReadings.Add(reading);
        return SaveAddedAsync([reading], cancellationToken);
    }

    private async Task InsertMissingAsync(string login, IReadOnlyCollection<Guid> documentIds, DateTimeOffset readAt, CancellationToken cancellationToken)
    {
        var pending = documentIds.Distinct().ToArray();
        for (var attempt = 0; attempt < 2 && pending.Length > 0; attempt++)
        {
            var existing = await db.DocumentReadings
                .Where(item => item.ParticipantLogin == login && pending.Contains(item.DocumentId))
                .Select(item => item.DocumentId)
                .ToListAsync(cancellationToken);
            var known = existing.ToHashSet();
            var added = new List<DocumentReading>();
            foreach (var documentId in pending)
            {
                if (!known.Add(documentId))
                {
                    continue;
                }

                var reading = DocumentReading.Open(Guid.CreateVersion7(), documentId, login, readAt);
                added.Add(reading);
                db.DocumentReadings.Add(reading);
            }

            if (added.Count == 0)
            {
                return;
            }

            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (DbUpdateException exception) when (DocumentReadingConflict.IsDuplicateReading(exception))
            {
                Detach(added);
                pending = added.Select(item => item.DocumentId).ToArray();
            }
            catch (DbUpdateException)
            {
                Detach(added);
                throw;
            }
        }
    }

    private async Task SaveAddedAsync(IReadOnlyList<DocumentReading> added, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (DocumentReadingConflict.IsDuplicateReading(exception))
        {
            Detach(added);
        }
        catch (DbUpdateException)
        {
            Detach(added);
            throw;
        }
    }

    private void Detach(DocumentReadBaseline baseline)
    {
        db.Entry(baseline).State = EntityState.Detached;
    }

    private void Detach(IEnumerable<DocumentReading> readings)
    {
        foreach (var reading in readings)
        {
            db.Entry(reading).State = EntityState.Detached;
        }
    }
}

internal static class DocumentReadingConflict
{
    internal const string ReadingConstraint = "IX_DocumentReadings_DocumentId_ParticipantLogin";

    internal const string BaselineConstraint = "PK_DocumentReadBaselines";

    public static bool IsDuplicateReading(Exception exception)
    {
        return IsUnique(exception, ReadingConstraint, "DocumentReadings.DocumentId", "DocumentReadings.ParticipantLogin");
    }

    public static bool IsDuplicateBaseline(Exception exception)
    {
        return IsUnique(exception, BaselineConstraint, "DocumentReadBaselines.Id");
    }

    private static bool IsUnique(Exception exception, string constraintName, params string[] sqliteColumns)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres)
            {
                return postgres.SqlState == PostgresErrorCodes.UniqueViolation
                    && postgres.ConstraintName == constraintName;
            }

            if (IsSqliteConstraint(current, sqliteColumns))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSqliteConstraint(Exception exception, string[] columns)
    {
        if (exception.GetType().FullName != "Microsoft.Data.Sqlite.SqliteException")
        {
            return false;
        }

        var extended = exception.GetType().GetProperty("SqliteExtendedErrorCode")?.GetValue(exception);
        if (extended is not int code || code != 2067)
        {
            return false;
        }

        return columns.All(column => exception.Message.Contains(column, StringComparison.Ordinal));
    }
}
