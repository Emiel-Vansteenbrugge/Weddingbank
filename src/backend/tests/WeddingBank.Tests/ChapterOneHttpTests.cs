using System.Net;
using System.Net.Http.Json;

namespace WeddingBank.Tests;

public sealed class ChapterOneHttpTests
{
    [Fact]
    public async Task Participants_reach_each_chapter_one_puzzle_only_through_http()
    {
        using var fixture = new CaseApiFixture();
        await fixture.InitializeChapterOneAsync();
        using var admin = await fixture.SignInAsync("administrator", CaseApiFixture.AdminPassword);
        using var juul = await fixture.SignInAsync("juul", CaseApiFixture.ClientPassword);
        using var amber = await fixture.SignInAsync("amber", CaseApiFixture.ClientPassword);

        var cases = await admin.GetFromJsonAsync<CaseSummary[]>("/api/admin/cases");
        var file = cases!.Single(item => item.Reference == "CH-I-001");
        var detail = await admin.GetFromJsonAsync<CaseDetail>($"/api/admin/cases/{file.Id}");
        var documents = detail!.Documents.ToDictionary(item => item.Reference, item => item.Id);
        var quests = await admin.GetFromJsonAsync<QuestSummary[]>("/api/admin/quests");
        Assert.Contains(quests!, item => item.Reference == "CH-I-QUEST-001");
        Assert.Contains(quests!, item => item.Reference == "CH-I-QUEST-002");
        var first = await admin.GetFromJsonAsync<QuestDetail>($"/api/admin/quests/{quests!.Single(item => item.Reference == "CH-I-QUEST-001").Id}");
        var second = await admin.GetFromJsonAsync<QuestDetail>($"/api/admin/quests/{quests!.Single(item => item.Reference == "CH-I-QUEST-002").Id}");
        Assert.False(first!.Tasks.Single().Completed);
        Assert.False(second!.Tasks.Single().Completed);
        Assert.Equal("DRAFT", DocumentStatuses(detail, "CH-I-DOC-007"));
        Assert.Equal("DRAFT", DocumentStatuses(detail, "CH-I-DOC-011"));

        Assert.Equal(["CH-I-DOC-001", "CH-I-DOC-002", "CH-I-DOC-003", "CH-I-DOC-004", "CH-I-DOC-006"], await Papers(juul));
        Assert.Equal(["CH-I-DOC-001", "CH-I-DOC-002", "CH-I-DOC-003", "CH-I-DOC-005", "CH-I-DOC-006"], await Papers(amber));
        Assert.Equal(["CH-I-QUEST-001"], await QuestReferences(juul));
        Assert.Equal(["CH-I-QUEST-001"], await QuestReferences(amber));
        Assert.Equal(HttpStatusCode.NotFound, (await juul.GetAsync($"/api/client/quests/{second.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await amber.GetAsync($"/api/client/quests/{second.Id}")).StatusCode);
        foreach (var reference in new[] { "CH-I-DOC-007", "CH-I-DOC-008", "CH-I-DOC-009", "CH-I-DOC-010", "CH-I-DOC-011" })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await juul.GetAsync($"/api/client/documents/{documents[reference]}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await amber.GetAsync($"/api/client/documents/{documents[reference]}")).StatusCode);
        }

        var hidden = await SendAsync<AnswerBody>(juul, $"/api/client/quests/tasks/{second.Tasks.Single().Id}/answers", new { answer = "Compliance" });
        Assert.Equal(HttpStatusCode.NotFound, hidden.Status);

        var refused = await SendAsync<AnswerBody>(juul, $"/api/client/quests/tasks/{first.Tasks.Single().Id}/answers", new { answer = "REG-1026-21" });
        Assert.Equal(HttpStatusCode.OK, refused.Status);
        Assert.False(refused.Body!.Accepted);
        Assert.Equal(["CH-I-QUEST-001"], await QuestReferences(amber));

        var solved = await SendAsync<AnswerBody>(amber, $"/api/client/quests/tasks/{first.Tasks.Single().Id}/answers", new { answer = "REG-1026-17" });
        Assert.Equal(HttpStatusCode.OK, solved.Status);
        Assert.True(solved.Body!.Accepted);
        Assert.True(solved.Body.Completed);
        Assert.Equal(["CH-I-QUEST-001", "CH-I-QUEST-002"], await QuestReferences(juul));
        Assert.Equal(["CH-I-QUEST-001", "CH-I-QUEST-002"], await QuestReferences(amber));
        Assert.Equal(HttpStatusCode.OK, (await juul.GetAsync($"/api/client/documents/{documents["CH-I-DOC-007"]}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await amber.GetAsync($"/api/client/documents/{documents["CH-I-DOC-007"]}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await juul.GetAsync($"/api/client/documents/{documents["CH-I-DOC-008"]}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await juul.GetAsync($"/api/client/documents/{documents["CH-I-DOC-010"]}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await juul.GetAsync($"/api/client/documents/{documents["CH-I-DOC-009"]}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await amber.GetAsync($"/api/client/documents/{documents["CH-I-DOC-009"]}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await amber.GetAsync($"/api/client/documents/{documents["CH-I-DOC-010"]}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await amber.GetAsync($"/api/client/documents/{documents["CH-I-DOC-008"]}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await juul.GetAsync($"/api/client/documents/{documents["CH-I-DOC-011"]}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await amber.GetAsync($"/api/client/documents/{documents["CH-I-DOC-011"]}")).StatusCode);
        var juulFirst = await juul.GetFromJsonAsync<ReadingQuest>($"/api/client/quests/{first.Id}");
        var amberFirst = await amber.GetFromJsonAsync<ReadingQuest>($"/api/client/quests/{first.Id}");
        var juulSecond = await juul.GetFromJsonAsync<ReadingQuest>($"/api/client/quests/{second.Id}");
        var amberSecond = await amber.GetFromJsonAsync<ReadingQuest>($"/api/client/quests/{second.Id}");
        Assert.Equal(1, juulFirst!.SharedCompleted);
        Assert.Equal(juulFirst.SharedCompleted, amberFirst!.SharedCompleted);
        Assert.Equal(0, juulFirst.SharedOpen);
        Assert.Equal(0, amberFirst.SharedOpen);
        Assert.True(juulFirst.Tasks.Single().Completed);
        Assert.True(amberFirst.Tasks.Single().Completed);
        Assert.Equal(0, juulSecond!.SharedCompleted);
        Assert.Equal(1, juulSecond.SharedOpen);
        Assert.Equal(juulSecond.SharedCompleted, amberSecond!.SharedCompleted);
        Assert.Equal(juulSecond.SharedOpen, amberSecond.SharedOpen);
        Assert.False(juulSecond.Tasks.Single().Completed);
        Assert.False(amberSecond.Tasks.Single().Completed);

        var wrongDepartment = await SendAsync<AnswerBody>(juul, $"/api/client/quests/tasks/{second.Tasks.Single().Id}/answers", new { answer = "Account Services" });
        Assert.Equal(HttpStatusCode.OK, wrongDepartment.Status);
        Assert.False(wrongDepartment.Body!.Accepted);
        Assert.Equal(HttpStatusCode.NotFound, (await amber.GetAsync($"/api/client/documents/{documents["CH-I-DOC-011"]}")).StatusCode);

        var accepted = await SendAsync<AnswerBody>(amber, $"/api/client/quests/tasks/{second.Tasks.Single().Id}/answers", new { answer = "Compliance" });
        Assert.Equal(HttpStatusCode.OK, accepted.Status);
        Assert.True(accepted.Body!.Accepted);
        Assert.True(accepted.Body.Completed);
        Assert.Equal(HttpStatusCode.OK, (await juul.GetAsync($"/api/client/documents/{documents["CH-I-DOC-011"]}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await amber.GetAsync($"/api/client/documents/{documents["CH-I-DOC-011"]}")).StatusCode);
        var juulDone = await juul.GetFromJsonAsync<ReadingQuest>($"/api/client/quests/{second.Id}");
        var amberDone = await amber.GetFromJsonAsync<ReadingQuest>($"/api/client/quests/{second.Id}");
        Assert.True(juulDone!.Tasks.Single().Completed);
        Assert.True(amberDone!.Tasks.Single().Completed);
        Assert.Equal(1, juulDone.SharedCompleted);
        Assert.Equal(1, amberDone.SharedCompleted);
        Assert.Equal(0, juulDone.SharedOpen);
        Assert.Equal(0, amberDone.SharedOpen);
        Assert.Equal(HttpStatusCode.Conflict, (await SendAsync<AnswerBody>(juul, $"/api/client/quests/tasks/{second.Tasks.Single().Id}/answers", new { answer = "Compliance" })).Status);

        var adminFirst = await admin.GetFromJsonAsync<QuestDetail>($"/api/admin/quests/{first.Id}");
        var adminSecond = await admin.GetFromJsonAsync<QuestDetail>($"/api/admin/quests/{second.Id}");
        Assert.True(adminFirst!.Tasks.Single().Completed);
        Assert.True(adminSecond!.Tasks.Single().Completed);
        var firstSubmissions = await admin.GetFromJsonAsync<SubmissionItem[]>($"/api/admin/quests/{first.Id}/submissions");
        var secondSubmissions = await admin.GetFromJsonAsync<SubmissionItem[]>($"/api/admin/quests/{second.Id}/submissions");
        Assert.Contains(firstSubmissions!, item => item.SubmittedBy == "Juul-V" && item.Outcome == "REFUSED" && item.Answer == "REG-1026-21");
        Assert.Contains(firstSubmissions!, item => item.SubmittedBy == "Amber-D" && item.Outcome == "ACCEPTED" && item.Answer == "REG-1026-17");
        Assert.Contains(secondSubmissions!, item => item.SubmittedBy == "Juul-V" && item.Outcome == "REFUSED" && item.Answer == "Account Services");
        Assert.Contains(secondSubmissions!, item => item.SubmittedBy == "Amber-D" && item.Outcome == "ACCEPTED" && item.Answer == "Compliance");
    }

    private static async Task<string[]> Papers(HttpClient client)
    {
        var papers = await client.GetFromJsonAsync<Named[]>("/api/client/documents");
        return papers!.Select(item => item.Reference).Order().ToArray();
    }

    private static async Task<string[]> QuestReferences(HttpClient client)
    {
        var quests = await client.GetFromJsonAsync<Named[]>("/api/client/quests");
        return quests!.Select(item => item.Reference).Order().ToArray();
    }

    private static string DocumentStatuses(CaseDetail detail, string reference)
    {
        return detail.Documents.Single(item => item.Reference == reference).Status;
    }

    private static async Task<(HttpStatusCode Status, T? Body)> SendAsync<T>(HttpClient client, string path, object payload)
    {
        var token = await client.GetFromJsonAsync<TokenBody>("/api/auth/csrf");
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(payload) };
        request.Headers.TryAddWithoutValidation("Origin", "http://localhost:5173");
        request.Headers.TryAddWithoutValidation("X-CSRF-TOKEN", token?.Token);
        var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return (response.StatusCode, default);
        }

        return (response.StatusCode, await response.Content.ReadFromJsonAsync<T>());
    }

    private sealed record TokenBody(string? Token);

    private sealed record Named(string Reference);

    private sealed record CaseSummary(Guid Id, string Reference);

    private sealed record CaseDetail(Guid Id, DocumentItem[] Documents);

    private sealed record DocumentItem(Guid Id, string Reference, string Status);

    private sealed record QuestSummary(Guid Id, string Reference);

    private sealed record QuestDetail(Guid Id, TaskItem[] Tasks);

    private sealed record TaskItem(Guid Id, string Reference, bool Completed);

    private sealed record ReadingQuest(int SharedCompleted, int SharedOpen, ReadingTask[] Tasks);

    private sealed record ReadingTask(string Reference, bool Completed);

    private sealed record AnswerBody(bool Accepted, bool Completed);

    private sealed record SubmissionItem(string SubmittedBy, string Answer, string Outcome);
}
