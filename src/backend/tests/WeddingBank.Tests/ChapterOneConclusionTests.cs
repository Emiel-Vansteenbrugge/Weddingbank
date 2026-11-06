using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WeddingBank.Application;
using WeddingBank.Application.Game;
using WeddingBank.Domain.Game;
using WeddingBank.Infrastructure.Game;
using WeddingBank.Infrastructure.Persistence;

namespace WeddingBank.Tests;

public sealed class ChapterOneConclusionTests
{
    [Fact]
    public void The_new_puzzles_have_one_answer_and_keep_personal_papers_incomplete()
    {
        var procedure = Rows("CH-I-DOC-014");
        var handling = procedure.Where(row => row[1] == "Interim logging").Select(row => row[0]).ToArray();
        var verification = procedure.Where(row => row[2] == "Exception without exhibit").Select(row => row[0]).ToArray();
        var status = procedure.Single(row => row[1] == "Interim logging" && row[2] == "Exception without exhibit");
        Assert.Equal("ELECTRONIC RECEIPT ONLY", status[0]);
        Assert.True(handling.Length > 1);
        Assert.True(verification.Length > 1);
        Assert.Contains("ELECTRONIC RECEIPT PENDING", handling);
        Assert.Contains("ADMINISTRATIVE RECEIPT OPEN", verification);
        Assert.True(procedure.Select(row => row[0]).Count(item => item.Contains("ELECTRONIC RECEIPT", StringComparison.Ordinal)) > 1);
        foreach (var item in procedure.Select(row => row[0]).Distinct())
            Assert.DoesNotContain(item, Text("CH-I-DOC-011"), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ELECTRONIC RECEIPT ONLY", Text("CH-I-DOC-012"), StringComparison.Ordinal);
        Assert.DoesNotContain("ELECTRONIC RECEIPT ONLY", Text("CH-I-DOC-013"), StringComparison.Ordinal);
        Assert.DoesNotContain("Exception without exhibit", Text("CH-I-DOC-012"), StringComparison.Ordinal);
        Assert.DoesNotContain("Interim logging", Text("CH-I-DOC-013"), StringComparison.Ordinal);
        Assert.DoesNotContain("Interim logging", Text("CH-I-DOC-011"), StringComparison.Ordinal);
        Assert.DoesNotContain("Exception without exhibit", Text("CH-I-DOC-011"), StringComparison.Ordinal);
        Assert.DoesNotContain("Interim logging", ChapterOneCatalog.ThirdQuestDescription, StringComparison.Ordinal);
        Assert.DoesNotContain("ELECTRONIC RECEIPT ONLY", ChapterOneCatalog.ThirdQuestDescription, StringComparison.Ordinal);
        Assert.Contains("Intake handling: Interim logging", Text("CH-I-DOC-012"), StringComparison.Ordinal);
        Assert.Contains("Inspection verification: Exception without exhibit", Text("CH-I-DOC-013"), StringComparison.Ordinal);
        Assert.Contains("EUR 250.00 remains unavailable", Text("CH-I-DOC-015"), StringComparison.Ordinal);
        Assert.Contains("does not establish fraud", Text("CH-I-DOC-015"), StringComparison.Ordinal);
        Assert.DoesNotContain("Chapter II", Text("CH-I-DOC-015"), StringComparison.Ordinal);

        var closures = Rows("ARCH-2019-003");
        var byDepartment = closures.Where(row => row[1] == "Client Records").Select(row => row[2]).ToArray();
        var byDate = closures.Where(row => row[0] == "14/03/2019").Select(row => row[2]).ToArray();
        Assert.True(byDepartment.Length > 1);
        Assert.True(byDate.Length > 1);
        Assert.Equal("CL-19", closures.Single(row => row[0] == "14/03/2019" && row[1] == "Client Records")[2]);
        Assert.DoesNotContain("CL-19", Text("ARCH-2019-001"), StringComparison.Ordinal);
        Assert.DoesNotContain("CL-19", Text("ARCH-2019-002"), StringComparison.Ordinal);
        Assert.DoesNotContain("14 March 2019", Text("ARCH-2019-001"), StringComparison.Ordinal);
        Assert.DoesNotContain("Client Records", Text("ARCH-2019-002"), StringComparison.Ordinal);
        Assert.Contains("Client Records", Text("ARCH-2019-001"), StringComparison.Ordinal);
        Assert.Contains("14 March 2019", Text("ARCH-2019-002"), StringComparison.Ordinal);

        var fees = Rows("FILE-2026-003");
        Assert.True(fees.Count(row => row[0].Contains("88", StringComparison.Ordinal)) > 1);
        Assert.True(fees.Count(row => row[1] == "Custody fee" && row[3] == "Reversal") > 1);
        Assert.Equal("E-88-R", fees.Single(row => row[0] == "E-88-R")[0]);
        Assert.DoesNotContain("E-88-R", Text("FILE-2026-001"), StringComparison.Ordinal);
        Assert.DoesNotContain("-R", Text("FILE-2026-001"), StringComparison.Ordinal);
        Assert.DoesNotContain("correct", Text("FILE-2026-001"), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("revers", Text("FILE-2026-001"), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("88", Text("FILE-2026-002"), StringComparison.Ordinal);
        Assert.Contains("Original booking reference: E-88", Text("FILE-2026-001"), StringComparison.Ordinal);
        Assert.Contains("followed by -R", Text("FILE-2026-002"), StringComparison.Ordinal);
        Assert.Contains("was not an allocation", Text("FILE-2026-004"), StringComparison.Ordinal);
        Assert.DoesNotContain("REG-1026-17", Text("FILE-2026-004"), StringComparison.Ordinal);
        Assert.DoesNotContain("EUR 250.00", Text("ARCH-2019-004"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Chapter_I_closes_on_the_third_main_task_and_side_quests_stay_optional()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<WeddingBankDbContext>().UseSqlite(connection).Options;
        var now = new DateTimeOffset(2026, 11, 6, 12, 0, 0, TimeSpan.Zero);
        await using var db = new WeddingBankDbContext(options);
        await db.Database.EnsureCreatedAsync();
        Assert.Empty((await new ChapterOneInitializer(db, new FixedTime(now)).EnsureAsync()).Conflicts);

        var quests = new QuestService(new EfQuestStore(db), new EfCaseStore(db), new FixedTime(now));
        var papers = new CaseService(new EfCaseStore(db), new EfQuestStore(db), new EfDocumentReadStore(db), new FixedTime(now));
        var progress = new EfGameStore(db);
        var documents = await db.CaseDocuments.ToDictionaryAsync(document => document.Reference, document => document.Id);
        var tasks = await db.QuestTasks.ToDictionaryAsync(task => task.Reference, task => task.Id);
        var third = await db.Quests.SingleAsync(quest => quest.Reference == "CH-I-QUEST-003");
        Assert.Equal(QuestKinds.Main, third.Kind);
        Assert.Equal(QuestKinds.Side, (await db.Quests.SingleAsync(quest => quest.Reference == "CH-I-QUEST-004")).Kind);
        Assert.Equal(QuestKinds.Side, (await db.Quests.SingleAsync(quest => quest.Reference == "CH-I-QUEST-005")).Kind);
        Assert.Equal(OperationStatus.NotFound, (await quests.ReadForAsync(Participants.Juul, third.Id, CancellationToken.None)).Status);

        var sideFirst = await quests.SubmitAsync(Participants.Juul, tasks["CH-I-TASK-004"], "CL-20", CancellationToken.None);
        Assert.False(sideFirst.Value!.Accepted);
        Assert.False((await quests.SubmitAsync(Participants.Amber, tasks["CH-I-TASK-004"], "CL-21", CancellationToken.None)).Value!.Accepted);
        var sideSolved = await quests.SubmitAsync(Participants.Amber, tasks["CH-I-TASK-004"], "cl-19", CancellationToken.None);
        Assert.True(sideSolved.Value!.Completed);
        Assert.Equal(OperationStatus.Ok, (await papers.ReadPaperAsync(Participants.Juul, documents["ARCH-2019-004"], CancellationToken.None)).Status);
        Assert.Equal(OperationStatus.Ok, (await papers.ReadPaperAsync(Participants.Amber, documents["ARCH-2019-004"], CancellationToken.None)).Status);
        Assert.Equal(OperationStatus.NotFound, (await papers.ReadPaperAsync(Participants.Juul, documents["CH-I-DOC-015"], CancellationToken.None)).Status);
        Assert.Equal(OperationStatus.NotFound, (await quests.ReadForAsync(Participants.Amber, third.Id, CancellationToken.None)).Status);
        var afterSide = await progress.ReadVisibleProgressAsync(Participants.Juul, CancellationToken.None);
        Assert.Equal(0, afterSide.SharedCompleted);
        Assert.Equal(1, afterSide.SharedOpen);

        Assert.True((await quests.SubmitAsync(Participants.Juul, tasks["CH-I-TASK-001"], "REG-1026-17", CancellationToken.None)).Value!.Completed);
        Assert.Equal(OperationStatus.NotFound, (await papers.ReadPaperAsync(Participants.Juul, documents["CH-I-DOC-012"], CancellationToken.None)).Status);
        Assert.Equal(OperationStatus.NotFound, (await papers.ReadPaperAsync(Participants.Amber, documents["CH-I-DOC-013"], CancellationToken.None)).Status);
        Assert.True((await quests.SubmitAsync(Participants.Amber, tasks["CH-I-TASK-002"], "Compliance", CancellationToken.None)).Value!.Completed);

        Assert.Equal(OperationStatus.Ok, (await papers.ReadPaperAsync(Participants.Juul, documents["CH-I-DOC-012"], CancellationToken.None)).Status);
        Assert.Equal(OperationStatus.NotFound, (await papers.ReadPaperAsync(Participants.Juul, documents["CH-I-DOC-013"], CancellationToken.None)).Status);
        Assert.Equal(OperationStatus.Ok, (await papers.ReadPaperAsync(Participants.Amber, documents["CH-I-DOC-013"], CancellationToken.None)).Status);
        Assert.Equal(OperationStatus.NotFound, (await papers.ReadPaperAsync(Participants.Amber, documents["CH-I-DOC-012"], CancellationToken.None)).Status);
        Assert.Equal(OperationStatus.Ok, (await papers.ReadPaperAsync(Participants.Juul, documents["CH-I-DOC-014"], CancellationToken.None)).Status);
        Assert.Equal(OperationStatus.Ok, (await papers.ReadPaperAsync(Participants.Amber, documents["CH-I-DOC-014"], CancellationToken.None)).Status);
        Assert.Equal(OperationStatus.NotFound, (await papers.ReadPaperAsync(Participants.Juul, documents["CH-I-DOC-015"], CancellationToken.None)).Status);

        var wrongStatus = await quests.SubmitAsync(Participants.Juul, tasks["CH-I-TASK-003"], "ELECTRONIC RECEIPT PENDING", CancellationToken.None);
        Assert.False(wrongStatus.Value!.Accepted);
        Assert.False((await quests.SubmitAsync(Participants.Amber, tasks["CH-I-TASK-003"], "ADMINISTRATIVE RECEIPT OPEN", CancellationToken.None)).Value!.Accepted);
        Assert.Equal(OperationStatus.NotFound, (await papers.ReadPaperAsync(Participants.Amber, documents["CH-I-DOC-015"], CancellationToken.None)).Status);
        var found = await quests.SubmitAsync(Participants.Amber, tasks["CH-I-TASK-003"], " electronic receipt only ", CancellationToken.None);
        Assert.True(found.Value!.Completed);
        Assert.Equal("The confirmation status has been recorded. The preliminary finding is now available for examination.", found.Value.Message);
        Assert.Equal(OperationStatus.Ok, (await papers.ReadPaperAsync(Participants.Juul, documents["CH-I-DOC-015"], CancellationToken.None)).Status);
        Assert.Equal(OperationStatus.Ok, (await papers.ReadPaperAsync(Participants.Amber, documents["CH-I-DOC-015"], CancellationToken.None)).Status);
        var closed = await progress.ReadVisibleProgressAsync(Participants.Amber, CancellationToken.None);
        Assert.Equal(3, closed.SharedCompleted);
        Assert.Equal(0, closed.SharedOpen);
        Assert.Equal(0, await db.CaseFiles.CountAsync(file => file.Reference.StartsWith("CH-II")));
        Assert.Equal(0, await db.Quests.CountAsync(quest => quest.Reference.StartsWith("CH-II")));

        Assert.False((await quests.SubmitAsync(Participants.Amber, tasks["CH-I-TASK-005"], "E-88", CancellationToken.None)).Value!.Accepted);
        Assert.False((await quests.SubmitAsync(Participants.Juul, tasks["CH-I-TASK-005"], "E-91-R", CancellationToken.None)).Value!.Accepted);
        var fee = await quests.SubmitAsync(Participants.Juul, tasks["CH-I-TASK-005"], "e-88-r", CancellationToken.None);
        Assert.True(fee.Value!.Completed);
        Assert.Equal(OperationStatus.Ok, (await papers.ReadPaperAsync(Participants.Amber, documents["FILE-2026-004"], CancellationToken.None)).Status);
        Assert.Equal(OperationStatus.NotFound, (await papers.ReadPaperAsync(Participants.Amber, documents["FILE-2026-001"], CancellationToken.None)).Status);
        Assert.Equal(OperationStatus.NotFound, (await papers.ReadPaperAsync(Participants.Juul, documents["FILE-2026-002"], CancellationToken.None)).Status);
        var stillClosed = await progress.ReadVisibleProgressAsync(Participants.Juul, CancellationToken.None);
        Assert.Equal(3, stillClosed.SharedCompleted);
        Assert.Equal(0, stillClosed.SharedOpen);
        Assert.Equal(5, await db.Quests.CountAsync());

        var kept = await new ChapterOneInitializer(db, new FixedTime(now.AddHours(1))).EnsureAsync();
        Assert.Empty(kept.Conflicts);
        Assert.DoesNotContain(kept.Lines, line => line.StartsWith("created ", StringComparison.Ordinal));
        Assert.Equal(5, await db.Assignments.CountAsync(item => item.Completed));
    }

    [Fact]
    public async Task Participants_do_not_receive_quest_kind_and_chapter_ii_stays_closed()
    {
        using var fixture = new CaseApiFixture();
        await fixture.InitializeChapterOneAsync();
        using var admin = await fixture.SignInAsync("administrator", CaseApiFixture.AdminPassword);
        using var juul = await fixture.SignInAsync("juul", CaseApiFixture.ClientPassword);
        using var amber = await fixture.SignInAsync("amber", CaseApiFixture.ClientPassword);

        var adminQuests = (await admin.GetFromJsonAsync<QuestKindItem[]>("/api/admin/quests"))!;
        Assert.Equal("MAIN", adminQuests.Single(item => item.Reference == "CH-I-QUEST-003").Kind);
        Assert.Equal("SIDE", adminQuests.Single(item => item.Reference == "CH-I-QUEST-004").Kind);
        Assert.Equal("SIDE", adminQuests.Single(item => item.Reference == "CH-I-QUEST-005").Kind);
        var participantList = await juul.GetStringAsync("/api/client/quests");
        Assert.DoesNotContain("\"kind\"", participantList, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SIDE", participantList, StringComparison.Ordinal);
        Assert.DoesNotContain("MAIN", participantList, StringComparison.Ordinal);

        var cases = await admin.GetFromJsonAsync<CaseItem[]>("/api/admin/cases");
        Assert.Contains(cases!, item => item.Reference == "ARCHIVE-2019-047");
        Assert.Contains(cases!, item => item.Reference == "FILE-2026-088");
        Assert.DoesNotContain(cases!, item => item.Reference.StartsWith("CH-II", StringComparison.Ordinal));
        var quests = await admin.GetFromJsonAsync<QuestItem[]>("/api/admin/quests");
        var third = await admin.GetFromJsonAsync<QuestDetail>($"/api/admin/quests/{quests!.Single(item => item.Reference == "CH-I-QUEST-003").Id}");
        Assert.Equal(HttpStatusCode.NotFound, (await juul.GetAsync($"/api/client/quests/{third!.Id}")).StatusCode);

        await SolveAsync(admin, amber, quests!, "CH-I-QUEST-001", "REG-1026-17");
        await SolveAsync(admin, juul, quests!, "CH-I-QUEST-002", "Compliance");
        var account = await admin.GetFromJsonAsync<CaseDetail>($"/api/admin/cases/{cases!.Single(item => item.Reference == "CH-I-001").Id}");
        var finding = account!.Documents.Single(item => item.Reference == "CH-I-DOC-015");
        Assert.Equal(HttpStatusCode.NotFound, (await juul.GetAsync($"/api/client/documents/{finding.Id}")).StatusCode);
        await SolveAsync(admin, juul, quests!, "CH-I-QUEST-003", "ELECTRONIC RECEIPT ONLY");
        Assert.Equal(HttpStatusCode.OK, (await juul.GetAsync($"/api/client/documents/{finding.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await amber.GetAsync($"/api/client/documents/{finding.Id}")).StatusCode);
        var after = await admin.GetFromJsonAsync<CaseItem[]>("/api/admin/cases");
        Assert.DoesNotContain(after!, item => item.Reference.StartsWith("CH-II", StringComparison.Ordinal));
        Assert.Equal(5, (await admin.GetFromJsonAsync<QuestItem[]>("/api/admin/quests"))!.Length);
    }

    private static async Task SolveAsync(HttpClient admin, HttpClient client, QuestItem[] quests, string reference, string answer)
    {
        var detail = await admin.GetFromJsonAsync<QuestDetail>($"/api/admin/quests/{quests.Single(item => item.Reference == reference).Id}");
        var token = await client.GetFromJsonAsync<TokenBody>("/api/auth/csrf");
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/client/quests/tasks/{detail!.Tasks.Single().Id}/answers")
        {
            Content = JsonContent.Create(new { answer }),
        };
        request.Headers.TryAddWithoutValidation("Origin", "http://localhost:5173");
        request.Headers.TryAddWithoutValidation("X-CSRF-TOKEN", token?.Token);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True((await response.Content.ReadFromJsonAsync<AnswerBody>())!.Accepted);
    }

    private static string Text(string reference)
    {
        return ChapterOneCatalog.Cases.SelectMany(item => item.Documents).Single(item => item.Reference == reference).Content;
    }

    private static string[][] Rows(string reference)
    {
        return Text(reference)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => line.Contains(" | ", StringComparison.Ordinal))
            .Skip(1)
            .Select(line => line.Split('|', StringSplitOptions.TrimEntries))
            .ToArray();
    }

    private sealed record QuestKindItem(string Reference, string Kind);

    private sealed record QuestItem(Guid Id, string Reference);

    private sealed record QuestDetail(Guid Id, TaskItem[] Tasks);

    private sealed record TaskItem(Guid Id);

    private sealed record CaseItem(Guid Id, string Reference);

    private sealed record CaseDetail(DocumentItem[] Documents);

    private sealed record DocumentItem(Guid Id, string Reference);

    private sealed record AnswerBody(bool Accepted);

    private sealed record TokenBody(string? Token);

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
