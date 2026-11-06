using Microsoft.EntityFrameworkCore;
using Npgsql;
using WeddingBank.Domain.Game;
using WeddingBank.Infrastructure.Game;
using WeddingBank.Infrastructure.Persistence;

namespace WeddingBank.Tests;

public sealed class DocumentReadConflictTests
{
    [Fact]
    public void Only_the_document_participant_unique_conflict_is_expected()
    {
        var reading = Postgres(PostgresErrorCodes.UniqueViolation, DocumentReadingConflict.ReadingConstraint);
        var otherConstraint = Postgres(PostgresErrorCodes.UniqueViolation, DocumentReadingConflict.BaselineConstraint);
        var foreignKey = Postgres(PostgresErrorCodes.ForeignKeyViolation, DocumentReadingConflict.ReadingConstraint);

        Assert.True(DocumentReadingConflict.IsDuplicateReading(new DbUpdateException("save failed", reading)));
        Assert.False(DocumentReadingConflict.IsDuplicateReading(new DbUpdateException("save failed", otherConstraint)));
        Assert.False(DocumentReadingConflict.IsDuplicateReading(new DbUpdateException("save failed", foreignKey)));
        Assert.False(DocumentReadingConflict.IsDuplicateReading(new DbUpdateException("save failed", innerException: null)));
    }

    [Fact]
    public async Task Two_registrations_of_the_same_reading_keep_one_row_and_other_changes()
    {
        await using var database = await ReadingDatabase.CreateAsync();
        var now = new DateTimeOffset(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);
        await using var db = database.Open();
        var store = new EfDocumentReadStore(db);
        var file = await db.CaseFiles.SingleAsync();

        await store.InsertReadingAsync(Participants.Juul, database.DocumentId, now, CancellationToken.None);
        file.Revise(file.Reference, "Revised title", file.Summary, file.Introduction, file.Audience, now);
        await store.InsertReadingAsync(Participants.Juul, database.DocumentId, now, CancellationToken.None);

        Assert.Equal(EntityState.Modified, db.Entry(file).State);
        Assert.Equal(1, await db.DocumentReadings.CountAsync(item => item.ParticipantLogin == Participants.Juul));
        await db.SaveChangesAsync();

        await using var check = database.Open();
        Assert.Equal("Revised title", (await check.CaseFiles.SingleAsync()).Title);
        Assert.Equal(1, await check.DocumentReadings.CountAsync());
    }

    [Fact]
    public async Task Concurrent_registrations_store_the_reading_once()
    {
        await using var database = await ReadingDatabase.CreateAsync();
        var now = new DateTimeOffset(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);

        await Task.WhenAll(
            RegisterAsync(database, Participants.Juul, now),
            RegisterAsync(database, Participants.Juul, now));

        await using var check = database.Open();
        Assert.Equal(1, await check.DocumentReadings.CountAsync(item => item.DocumentId == database.DocumentId && item.ParticipantLogin == Participants.Juul));
    }

    [Fact]
    public async Task A_foreign_key_failure_is_not_ignored_and_the_context_can_save_again()
    {
        await using var database = await ReadingDatabase.CreateAsync();
        var now = new DateTimeOffset(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);
        await using var db = database.Open();
        var store = new EfDocumentReadStore(db);

        await Assert.ThrowsAsync<DbUpdateException>(() => store.MarkReadAsync(Participants.Juul, Guid.CreateVersion7(), now, CancellationToken.None));
        await store.MarkReadAsync(Participants.Juul, database.DocumentId, now, CancellationToken.None);

        Assert.Equal(1, await db.DocumentReadings.CountAsync());
        Assert.Equal(database.DocumentId, (await db.DocumentReadings.SingleAsync()).DocumentId);
    }

    [Fact]
    public async Task Juul_and_Amber_keep_separate_readings_for_the_same_document()
    {
        await using var database = await ReadingDatabase.CreateAsync();
        var now = new DateTimeOffset(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);
        await using var db = database.Open();
        var store = new EfDocumentReadStore(db);

        await store.MarkReadAsync(Participants.Juul, database.DocumentId, now, CancellationToken.None);
        await store.MarkReadAsync(Participants.Amber, database.DocumentId, now, CancellationToken.None);
        await store.MarkReadAsync(Participants.Juul, database.DocumentId, now.AddMinutes(1), CancellationToken.None);

        var readings = await db.DocumentReadings.OrderBy(item => item.ParticipantLogin).ToListAsync();
        Assert.Equal([Participants.Amber, Participants.Juul], readings.Select(item => item.ParticipantLogin).ToArray());
        Assert.Equal(now, readings.Single(item => item.ParticipantLogin == Participants.Juul).ReadAt);
    }

    private static async Task RegisterAsync(ReadingDatabase database, string login, DateTimeOffset now)
    {
        await using var db = database.Open();
        await new EfDocumentReadStore(db).MarkReadAsync(login, database.DocumentId, now, CancellationToken.None);
    }

    private static PostgresException Postgres(string sqlState, string constraintName)
    {
        return new PostgresException(
            "database rejected the row",
            "ERROR",
            "ERROR",
            sqlState,
            constraintName: constraintName);
    }

    private sealed class ReadingDatabase : IAsyncDisposable
    {
        private readonly string _path;

        private ReadingDatabase(string path, Guid documentId)
        {
            _path = path;
            DocumentId = documentId;
        }

        public Guid DocumentId { get; }

        public static async Task<ReadingDatabase> CreateAsync()
        {
            var path = Path.Combine(Path.GetTempPath(), $"wb-reads-{Guid.NewGuid():N}.db");
            var database = new ReadingDatabase(path, Guid.CreateVersion7());
            await using var db = database.Open();
            await db.Database.EnsureCreatedAsync();
            var now = new DateTimeOffset(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);
            var file = CaseFile.Open(Guid.CreateVersion7(), "CASE-READ", "Title", "Summary", null, Audiences.Both, 0, now);
            file.AddDocument(database.DocumentId, "DOC-READ", "Note", "Note", null, "Text", Audiences.Both, 0, now);
            db.CaseFiles.Add(file);
            await db.SaveChangesAsync();
            return database;
        }

        public WeddingBankDbContext Open()
        {
            var options = new DbContextOptionsBuilder<WeddingBankDbContext>()
                .UseSqlite($"Data Source={_path};Pooling=False;Default Timeout=5")
                .Options;
            return new WeddingBankDbContext(options);
        }

        public async ValueTask DisposeAsync()
        {
            await using var db = Open();
            await db.Database.EnsureDeletedAsync();
        }
    }
}
