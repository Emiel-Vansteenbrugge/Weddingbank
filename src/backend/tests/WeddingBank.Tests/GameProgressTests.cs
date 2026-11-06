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

    [Fact]
    public async Task Participant_progress_counts_only_published_work_that_participant_can_open()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<WeddingBankDbContext>().UseSqlite(connection).Options;
        var now = new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);

        await using (var db = new WeddingBankDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
            var file = PublishedCase(db, "CH-I-001", Audiences.Both, now);
            AddWork(db, file, "CH-I-QUEST-001", "CH-I-TASK-001", Audiences.Both, questPublished: true, taskPublished: true, shared: true, completed: false, now);
            AddWork(db, file, "CH-I-QUEST-003", "CH-I-TASK-003", Audiences.Both, questPublished: true, taskPublished: true, shared: true, completed: true, now);
            AddWork(db, file, "CH-I-QUEST-002", "CH-I-TASK-002", Audiences.Both, questPublished: false, taskPublished: true, shared: true, completed: false, now);
            AddWork(db, file, "CH-I-QUEST-004", "CH-I-TASK-004", Audiences.Both, questPublished: true, taskPublished: false, shared: true, completed: false, now);
            AddWork(db, file, "CH-I-QUEST-005", "CH-I-TASK-005", Audiences.Juul, questPublished: true, taskPublished: true, shared: true, completed: false, now);
            var hiddenCase = PublishedCase(db, "CH-I-009", Audiences.Both, now);
            hiddenCase.SetStatus(CaseStatuses.Draft, now);
            AddWork(db, hiddenCase, "CH-I-QUEST-009", "CH-I-TASK-009", Audiences.Both, questPublished: true, taskPublished: true, shared: true, completed: false, now);
            var juulTask = AddWork(db, file, "CH-I-QUEST-006", "CH-I-TASK-006", Audiences.Both, questPublished: true, taskPublished: true, shared: false, completed: true, now);
            juulTask.Complete(now);
            await db.SaveChangesAsync();
        }

        await using var read = new WeddingBankDbContext(options);
        var store = new EfGameStore(read);
        var juul = await store.ReadVisibleProgressAsync(Participants.Juul, CancellationToken.None);
        var amber = await store.ReadVisibleProgressAsync(Participants.Amber, CancellationToken.None);
        var house = await store.ReadCountsAsync(CancellationToken.None);

        Assert.Equal(2, juul.SharedOpen);
        Assert.Equal(1, amber.SharedOpen);
        Assert.Equal(1, juul.SharedCompleted);
        Assert.Equal(1, amber.SharedCompleted);
        Assert.Equal(1, juul.Completed);
        Assert.Equal(0, juul.Open);
        Assert.Equal(0, amber.Completed);
        Assert.Equal(0, amber.Open);
        Assert.Equal(7, await read.Assignments.CountAsync());
        Assert.True(house.SharedOpen > juul.SharedOpen);
    }

    private static CaseFile PublishedCase(WeddingBankDbContext db, string reference, string audience, DateTimeOffset now)
    {
        var file = CaseFile.Open(Guid.CreateVersion7(), reference, "The Account", "A file for the house.", null, audience, 0, now);
        file.SetStatus(CaseStatuses.Published, now);
        db.CaseFiles.Add(file);
        return file;
    }

    private static Assignment AddWork(
        WeddingBankDbContext db,
        CaseFile file,
        string questReference,
        string taskReference,
        string audience,
        bool questPublished,
        bool taskPublished,
        bool shared,
        bool completed,
        DateTimeOffset now)
    {
        var quest = Quest.Open(Guid.CreateVersion7(), questReference, "A quest", "The house is still reading.", "Look at the records.", file.Id, audience, QuestKinds.Main, 0, now);
        if (questPublished)
        {
            quest.SetStatus(CaseStatuses.Published, now);
        }

        var task = quest.AddTask(Guid.CreateVersion7(), taskReference, "A task", "Submit the reference.", TaskTypes.Text, shared ? ProgressScope.Shared : ProgressScope.Individual, shared ? null : Participants.Juul, true, true, null, 0, now);
        if (taskPublished)
        {
            quest.SetTaskStatus(task.Id, TaskStatuses.Published, now);
        }

        var assignment = shared
            ? Assignment.ForTheHouse(Guid.CreateVersion7(), taskReference, now, task.Id)
            : Assignment.ForParticipant(Guid.CreateVersion7(), taskReference, Participants.Juul, now, task.Id);
        if (completed)
        {
            assignment.Complete(now);
        }

        db.Quests.Add(quest);
        db.Assignments.Add(assignment);
        return assignment;
    }
}
