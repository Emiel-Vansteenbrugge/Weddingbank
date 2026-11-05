using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WeddingBank.Application;
using WeddingBank.Application.Game;
using WeddingBank.Domain.Game;
using WeddingBank.Infrastructure.Game;
using WeddingBank.Infrastructure.Persistence;

namespace WeddingBank.Tests;

public sealed class ChapterOnePuzzleTests
{
    [Fact]
    public void Each_player_needs_the_other_personal_paper_to_identify_one_answer()
    {
        var register = Rows("CH-I-DOC-006", "REG-");
        var byDepartment = register.Where(row => row.Second == "Records Administration").ToArray();
        var byDate = register.Where(row => row.Third == "29/10/2026").ToArray();
        var together = register.Where(row => row.Second == "Records Administration" && row.Third == "29/10/2026").ToArray();

        Assert.DoesNotContain("29 October 2026", Text("CH-I-DOC-003"), StringComparison.Ordinal);
        Assert.DoesNotContain("Records Administration", Text("CH-I-DOC-003"), StringComparison.Ordinal);
        Assert.DoesNotContain("Records Administration", Text("CH-I-DOC-005"), StringComparison.Ordinal);
        Assert.Contains("processed by Records Administration", Text("CH-I-DOC-004"), StringComparison.Ordinal);
        Assert.Contains("29 October 2026", Text("CH-I-DOC-005"), StringComparison.Ordinal);
        Assert.True(byDepartment.Length > 1);
        Assert.True(byDate.Length > 1);
        Assert.Equal("REG-1026-17", Assert.Single(together).First);

        var transfers = Rows("CH-I-DOC-010", "TR-");
        var batch = transfers.Where(row => row.First.StartsWith("TR-1030-", StringComparison.Ordinal)).ToArray();
        var category = transfers.Where(row => row.Second == "B").ToArray();
        var transfer = transfers.Single(row => row.First.StartsWith("TR-1030-", StringComparison.Ordinal) && row.Second == "B");
        Assert.DoesNotContain("Compliance", Text("CH-I-DOC-008"), StringComparison.Ordinal);
        Assert.DoesNotContain("Compliance", Text("CH-I-DOC-009"), StringComparison.Ordinal);
        Assert.Contains("TR-1030", Text("CH-I-DOC-008"), StringComparison.Ordinal);
        Assert.Contains("routing category B", Text("CH-I-DOC-009"), StringComparison.Ordinal);
        Assert.True(batch.Length > 1);
        Assert.True(category.Length > 1);
        Assert.Equal("TR-1030-B", transfer.First);
        Assert.Equal("Compliance", transfer.Third);
    }

    [Fact]
    public async Task Chapter_I_opens_the_second_puzzle_only_after_the_first_is_solved()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<WeddingBankDbContext>().UseSqlite(connection).Options;
        var now = new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);
        await using var db = new WeddingBankDbContext(options);
        await db.Database.EnsureCreatedAsync();
        Assert.Empty((await new ChapterOneInitializer(db, new FixedTime(now)).EnsureAsync()).Conflicts);

        var store = new EfQuestStore(db);
        var cases = new EfCaseStore(db);
        var clock = new FixedTime(now);
        var quests = new QuestService(store, cases, clock);
        var papers = new CaseService(cases, store, clock);
        var progress = new EfGameStore(db);
        var documents = await db.CaseDocuments.ToDictionaryAsync(document => document.Reference, document => document.Id);
        var tasks = await db.QuestTasks.ToDictionaryAsync(task => task.Reference, task => task.Id);
        var secondQuest = await db.Quests.SingleAsync(quest => quest.Reference == "CH-I-QUEST-002");

        Assert.Equal(["CH-I-DOC-001", "CH-I-DOC-002", "CH-I-DOC-003", "CH-I-DOC-004", "CH-I-DOC-006"], await VisiblePapers(papers, Participants.Juul));
        Assert.Equal(["CH-I-DOC-001", "CH-I-DOC-002", "CH-I-DOC-003", "CH-I-DOC-005", "CH-I-DOC-006"], await VisiblePapers(papers, Participants.Amber));
        Assert.Equal(["CH-I-QUEST-001"], await VisibleQuests(quests, Participants.Juul));
        Assert.Equal(OperationStatus.NotFound, (await quests.ReadForAsync(Participants.Juul, secondQuest.Id, CancellationToken.None)).Status);
        Assert.Equal(OperationStatus.NotFound, (await papers.ReadPaperAsync(Participants.Juul, documents["CH-I-DOC-008"], CancellationToken.None)).Status);
        Assert.Equal(OperationStatus.NotFound, (await papers.ReadPaperAsync(Participants.Amber, documents["CH-I-DOC-009"], CancellationToken.None)).Status);
        Assert.Equal(OperationStatus.NotFound, (await quests.SubmitAsync(Participants.Juul, tasks["CH-I-TASK-002"], "Compliance", CancellationToken.None)).Status);
        var hidden = await progress.ReadVisibleProgressAsync(Participants.Juul, CancellationToken.None);
        var house = await progress.ReadCountsAsync(CancellationToken.None);
        Assert.Equal(1, hidden.SharedOpen);
        Assert.Equal(0, hidden.SharedCompleted);
        Assert.Equal(2, house.SharedOpen);

        var wrong = await quests.SubmitAsync(Participants.Juul, tasks["CH-I-TASK-001"], "REG-1026-21", CancellationToken.None);
        Assert.False(wrong.Value!.Accepted);
        Assert.Equal(["CH-I-QUEST-001"], await VisibleQuests(quests, Participants.Amber));
        Assert.Equal(OperationStatus.NotFound, (await papers.ReadPaperAsync(Participants.Amber, documents["CH-I-DOC-007"], CancellationToken.None)).Status);

        var solved = await quests.SubmitAsync(Participants.Amber, tasks["CH-I-TASK-001"], "REG-1026-17", CancellationToken.None);
        Assert.True(solved.Value!.Accepted);
        Assert.True(solved.Value.Completed);
        Assert.Equal(["CH-I-QUEST-001", "CH-I-QUEST-002"], await VisibleQuests(quests, Participants.Juul));
        Assert.Equal(["CH-I-QUEST-001", "CH-I-QUEST-002"], await VisibleQuests(quests, Participants.Amber));
        Assert.Equal(OperationStatus.Ok, (await papers.ReadPaperAsync(Participants.Juul, documents["CH-I-DOC-007"], CancellationToken.None)).Status);
        Assert.Equal(OperationStatus.Ok, (await papers.ReadPaperAsync(Participants.Juul, documents["CH-I-DOC-008"], CancellationToken.None)).Status);
        Assert.Equal(OperationStatus.Ok, (await papers.ReadPaperAsync(Participants.Juul, documents["CH-I-DOC-010"], CancellationToken.None)).Status);
        Assert.Equal(OperationStatus.NotFound, (await papers.ReadPaperAsync(Participants.Juul, documents["CH-I-DOC-009"], CancellationToken.None)).Status);
        Assert.Equal(OperationStatus.Ok, (await papers.ReadPaperAsync(Participants.Amber, documents["CH-I-DOC-009"], CancellationToken.None)).Status);
        Assert.Equal(OperationStatus.NotFound, (await papers.ReadPaperAsync(Participants.Amber, documents["CH-I-DOC-008"], CancellationToken.None)).Status);
        Assert.Equal(OperationStatus.NotFound, (await papers.ReadPaperAsync(Participants.Juul, documents["CH-I-DOC-011"], CancellationToken.None)).Status);
        var opened = await progress.ReadVisibleProgressAsync(Participants.Amber, CancellationToken.None);
        Assert.Equal(1, opened.SharedCompleted);
        Assert.Equal(1, opened.SharedOpen);

        var refused = await quests.SubmitAsync(Participants.Juul, tasks["CH-I-TASK-002"], "Account Services", CancellationToken.None);
        Assert.False(refused.Value!.Accepted);
        Assert.Equal(OperationStatus.NotFound, (await papers.ReadPaperAsync(Participants.Amber, documents["CH-I-DOC-011"], CancellationToken.None)).Status);

        var accepted = await quests.SubmitAsync(Participants.Amber, tasks["CH-I-TASK-002"], " compliance ", CancellationToken.None);
        Assert.True(accepted.Value!.Accepted);
        Assert.True(accepted.Value.Completed);
        Assert.Equal("The answer has been accepted.", accepted.Value.Message);
        Assert.Equal(OperationStatus.Ok, (await papers.ReadPaperAsync(Participants.Juul, documents["CH-I-DOC-011"], CancellationToken.None)).Status);
        Assert.Equal(OperationStatus.Ok, (await papers.ReadPaperAsync(Participants.Amber, documents["CH-I-DOC-011"], CancellationToken.None)).Status);
        Assert.Equal(OperationStatus.Conflict, (await quests.SubmitAsync(Participants.Juul, tasks["CH-I-TASK-002"], "Compliance", CancellationToken.None)).Status);
        var done = await progress.ReadVisibleProgressAsync(Participants.Juul, CancellationToken.None);
        Assert.Equal(2, done.SharedCompleted);
        Assert.Equal(0, done.SharedOpen);
        Assert.Equal(2, await db.Assignments.CountAsync(item => item.Completed));
        Assert.Equal(4, await db.Submissions.CountAsync());

        var again = await new ChapterOneInitializer(db, new FixedTime(now.AddHours(1))).EnsureAsync();
        Assert.Empty(again.Conflicts);
        Assert.Equal(2, await db.Assignments.CountAsync(item => item.Completed));
        Assert.Equal(4, await db.Submissions.CountAsync());
        Assert.Equal(2, await db.Quests.CountAsync());
        Assert.Equal(11, await db.CaseDocuments.CountAsync());
    }

    private static async Task<string[]> VisiblePapers(CaseService papers, string login)
    {
        var list = await papers.PapersForAsync(login, CancellationToken.None);
        return list.Select(paper => paper.Reference).Order(StringComparer.Ordinal).ToArray();
    }

    private static async Task<string[]> VisibleQuests(QuestService quests, string login)
    {
        var list = await quests.ListForAsync(login, CancellationToken.None);
        return list.Select(quest => quest.Reference).Order(StringComparer.Ordinal).ToArray();
    }

    private static string Text(string reference)
    {
        return ChapterOneCatalog.Documents.Single(document => document.Reference == reference).Content;
    }

    private static (string First, string Second, string Third)[] Rows(string reference, string prefix)
    {
        return Text(reference)
            .Split('\n')
            .Where(line => line.StartsWith(prefix, StringComparison.Ordinal))
            .Select(line => line.Split(" | "))
            .Select(parts => (parts[0], parts[1], parts[2]))
            .ToArray();
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
