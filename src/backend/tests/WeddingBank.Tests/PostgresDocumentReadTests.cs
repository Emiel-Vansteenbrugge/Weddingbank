using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using WeddingBank.Domain.Game;
using WeddingBank.Infrastructure.Game;
using WeddingBank.Infrastructure.Persistence;

namespace WeddingBank.Tests;

/// <summary>
/// Opt-in PostgreSQL integration tests for document read conflicts.
/// Set <c>WEDDINGBANK_POSTGRES_TEST</c> to a connection string for PostgreSQL 18 on a loopback host.
/// The database name must contain "test" and must not be "weddingbank".
/// These tests never open the Weddingbank development or production database.
/// </summary>
[Collection(PostgresDocumentReadCollection.Name)]
public sealed class PostgresDocumentReadTests
{
    private static readonly DateTimeOffset ReadAt = new(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);

    [PostgresDocumentReadFact]
    public async Task Concurrent_reads_of_the_same_document_store_one_row_for_each_participant()
    {
        await using var data = await ReadingFixture.SeedAsync();
        var gate = new OverlapSaveInterceptor();

        await Task.WhenAll(
            MarkAsync(data.DocumentId, Participants.Juul, gate),
            MarkAsync(data.DocumentId, Participants.Juul, gate));

        await using (var db = ReadingFixture.Open())
        {
            var readings = await db.DocumentReadings.Where(item => item.DocumentId == data.DocumentId).ToListAsync();
            var juul = Assert.Single(readings);
            Assert.Equal(Participants.Juul, juul.ParticipantLogin);

            db.DocumentReadings.Add(DocumentReading.Open(Guid.CreateVersion7(), data.DocumentId, Participants.Juul, ReadAt));
            var duplicate = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            var postgres = Postgres(duplicate);
            Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
            Assert.Equal(DocumentReadingConflict.ReadingConstraint, postgres.ConstraintName);
        }

        await MarkAsync(data.DocumentId, Participants.Amber, interceptor: null);

        await using (var db = ReadingFixture.Open())
        {
            var readings = await db.DocumentReadings
                .Where(item => item.DocumentId == data.DocumentId)
                .OrderBy(item => item.ParticipantLogin)
                .ToListAsync();
            Assert.Equal([Participants.Amber, Participants.Juul], readings.Select(item => item.ParticipantLogin).ToArray());
        }
    }

    [PostgresDocumentReadFact]
    public async Task A_foreign_key_failure_is_raised_by_postgresql()
    {
        await using var data = await ReadingFixture.SeedAsync();
        await using var db = ReadingFixture.Open();
        var store = new EfDocumentReadStore(db);

        var failure = await Assert.ThrowsAsync<DbUpdateException>(() => store.MarkReadAsync(Participants.Juul, Guid.CreateVersion7(), ReadAt, CancellationToken.None));
        var postgres = Postgres(failure);
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, postgres.SqlState);
        Assert.NotEqual(DocumentReadingConflict.ReadingConstraint, postgres.ConstraintName);

        await store.MarkReadAsync(Participants.Juul, data.DocumentId, ReadAt, CancellationToken.None);
        Assert.Equal(data.DocumentId, (await db.DocumentReadings.SingleAsync()).DocumentId);
    }

    private static async Task MarkAsync(Guid documentId, string login, OverlapSaveInterceptor? interceptor)
    {
        await using var db = ReadingFixture.Open(interceptor);
        await new EfDocumentReadStore(db).MarkReadAsync(login, documentId, ReadAt, CancellationToken.None);
    }

    private static PostgresException Postgres(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres)
            {
                return postgres;
            }
        }

        throw new InvalidOperationException("The database error did not come from PostgreSQL.", exception);
    }

    private sealed class ReadingFixture : IAsyncDisposable
    {
        private ReadingFixture(Guid caseId, Guid documentId)
        {
            CaseId = caseId;
            DocumentId = documentId;
        }

        private Guid CaseId { get; }

        public Guid DocumentId { get; }

        public static async Task<ReadingFixture> SeedAsync()
        {
            await EnsureSchemaAsync();
            var fixture = new ReadingFixture(Guid.CreateVersion7(), Guid.CreateVersion7());
            var reference = "PG" + Guid.NewGuid().ToString("N")[..8];
            await using var db = Open();
            var file = CaseFile.Open(fixture.CaseId, reference, "Title", "Summary", null, Audiences.Both, 0, ReadAt);
            file.AddDocument(fixture.DocumentId, "DOC-PG", "Note", "Note", null, "Text", Audiences.Both, 0, ReadAt);
            db.CaseFiles.Add(file);
            await db.SaveChangesAsync();
            return fixture;
        }

        public static WeddingBankDbContext Open(SaveChangesInterceptor? interceptor = null)
        {
            var options = new DbContextOptionsBuilder<WeddingBankDbContext>().UseNpgsql(Connection.Value);
            if (interceptor is not null)
            {
                options.AddInterceptors(interceptor);
            }

            return new WeddingBankDbContext(options.Options);
        }

        public async ValueTask DisposeAsync()
        {
            await using var db = Open();
            var file = await db.CaseFiles.SingleOrDefaultAsync(item => item.Id == CaseId);
            if (file is null)
            {
                return;
            }

            db.CaseFiles.Remove(file);
            await db.SaveChangesAsync();
        }

        private static async Task EnsureSchemaAsync()
        {
            await Schema.Gate.WaitAsync();
            try
            {
                if (Schema.Ready)
                {
                    return;
                }

                await using var db = Open();
                await db.Database.OpenConnectionAsync();
                await using var command = db.Database.GetDbConnection().CreateCommand();
                command.CommandText = "SHOW server_version_num";
                var version = Convert.ToInt32(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
                if (version / 10000 != 18)
                {
                    throw new InvalidOperationException("The document-reading integration test requires PostgreSQL 18, the same major version as the Weddingbank environment.");
                }

                await db.Database.CloseConnectionAsync();
                await db.Database.EnsureCreatedAsync();
                Schema.Ready = true;
            }
            finally
            {
                Schema.Gate.Release();
            }
        }
    }

    private static class Schema
    {
        public static readonly SemaphoreSlim Gate = new(1, 1);

        public static bool Ready { get; set; }
    }

    private static class Connection
    {
        public static readonly string Value = Require();

        private static string Require()
        {
            var value = Environment.GetEnvironmentVariable(PostgresDocumentReadFactAttribute.Variable);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Set {PostgresDocumentReadFactAttribute.Variable} before running the PostgreSQL document-reading tests.");
            }

            var builder = new NpgsqlConnectionStringBuilder(value);
            if (builder.Host is not ("127.0.0.1" or "localhost" or "::1"))
            {
                throw new InvalidOperationException("The PostgreSQL document-reading test only connects to a loopback host.");
            }

            if (string.IsNullOrWhiteSpace(builder.Database)
                || builder.Database.Equals("weddingbank", StringComparison.OrdinalIgnoreCase)
                || !builder.Database.Contains("test", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Use a dedicated test database whose name contains \"test\" and is not weddingbank.");
            }

            return builder.ConnectionString;
        }
    }

    /// <summary>
    /// Holds the first two saves until both are inside <c>SaveChanges</c>, so both inserts pass the existence check.
    /// Later saves, including the retry after a unique conflict, proceed immediately.
    /// </summary>
    private sealed class OverlapSaveInterceptor : SaveChangesInterceptor
    {
        private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private int _waiting;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var arrived = Interlocked.Increment(ref _waiting);
            if (arrived <= 2)
            {
                if (arrived == 2)
                {
                    _ready.TrySetResult();
                }

                await _ready.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            }

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}

public static class PostgresDocumentReadCollection
{
    public const string Name = "PostgreSQL document readings";
}

[CollectionDefinition(PostgresDocumentReadCollection.Name, DisableParallelization = true)]
public sealed class PostgresDocumentReadCollectionDefinition;

public sealed class PostgresDocumentReadFactAttribute : FactAttribute
{
    public const string Variable = "WEDDINGBANK_POSTGRES_TEST";

    public PostgresDocumentReadFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(Variable)))
        {
            Skip = $"Opt-in: set {Variable} to a loopback PostgreSQL 18 connection string. The database name must contain \"test\" and must not be weddingbank.";
        }
    }
}
