using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WeddingBank.Domain.Game;
using WeddingBank.Infrastructure.Game;
using WeddingBank.Infrastructure.Persistence;

namespace WeddingBank.Tests;

public sealed class GameProgressTests
{
    [Fact]
    public void Completing_an_individual_assignment_leaves_the_other_participant_and_the_shared_work()
    {
        var now = new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);
        var juul = Assignment.ForParticipant(Guid.CreateVersion7(), "door", Participants.Juul, now);
        var amber = Assignment.ForParticipant(Guid.CreateVersion7(), "door", Participants.Amber, now);
        var shared = Assignment.ForTheHouse(Guid.CreateVersion7(), "door", now);
        var hint = Notice.Hint(Guid.CreateVersion7(), Participants.Juul, "Look again.", now);
        var both = Notice.Message(Guid.CreateVersion7(), null, "The house writes to you both.", now);

        juul.Complete(now);

        Assert.True(juul.Completed);
        Assert.False(amber.Completed);
        Assert.False(shared.Completed);
        Assert.True(juul.CountsFor(Participants.Juul));
        Assert.False(juul.CountsFor(Participants.Amber));
        Assert.True(shared.CountsFor(Participants.Amber));
        Assert.True(hint.IsFor(Participants.Juul));
        Assert.False(hint.IsFor(Participants.Amber));
        Assert.True(both.IsFor(Participants.Amber));
        Assert.False(Participants.IsParticipant("administrator"));
    }

    [Fact]
    public async Task Stored_progress_keeps_individual_and_shared_rows_apart()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<WeddingBankDbContext>().UseSqlite(connection).Options;
        var now = new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

        await using (var db = new WeddingBankDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
            var juul = Assignment.ForParticipant(Guid.CreateVersion7(), "door", Participants.Juul, now);
            juul.Complete(now);
            db.Assignments.Add(juul);
            db.Assignments.Add(Assignment.ForParticipant(Guid.CreateVersion7(), "door", Participants.Amber, now));
            db.Assignments.Add(Assignment.ForTheHouse(Guid.CreateVersion7(), "door", now));
            db.Notices.Add(Notice.Hint(Guid.CreateVersion7(), Participants.Juul, "Look again.", now));
            await db.SaveChangesAsync();
        }

        await using var read = new WeddingBankDbContext(options);
        var counts = await new EfGameStore(read).ReadCountsAsync(CancellationToken.None);
        Assert.Equal(1, counts.JuulCompleted);
        Assert.Equal(0, counts.JuulOpen);
        Assert.Equal(0, counts.AmberCompleted);
        Assert.Equal(1, counts.AmberOpen);
        Assert.Equal(0, counts.SharedCompleted);
        Assert.Equal(1, counts.SharedOpen);
        Assert.Equal(1, counts.HintsForJuul);
        Assert.Equal(0, counts.HintsForAmber);
        Assert.Equal(0, counts.HintsForBoth);
    }
}
