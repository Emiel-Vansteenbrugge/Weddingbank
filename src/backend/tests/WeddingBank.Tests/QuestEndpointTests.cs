using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace WeddingBank.Tests;

public sealed class QuestEndpointTests : IClassFixture<CaseApiFixture>
{
    private readonly CaseApiFixture _fixture;

    public QuestEndpointTests(CaseApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task A_shared_answer_opens_the_locked_document_for_both_participants()
    {
        using var admin = await _fixture.SignInAsync("administrator", CaseApiFixture.AdminPassword);
        var file = await CreateCaseAsync(admin, "CH-I-001", "BOTH");
        var openPaper = await CreateDocumentAsync(admin, file.Id, "CH-I-DOC-006", "BOTH");
        await SendAsync<DocumentBody>(admin, HttpMethod.Post, $"/api/admin/cases/{file.Id}/documents/{openPaper.Id}/status", new { status = "PUBLISHED" });
        var locked = await CreateDocumentAsync(admin, file.Id, "CH-I-DOC-007", "BOTH");
        await SendAsync<CaseBody>(admin, HttpMethod.Post, $"/api/admin/cases/{file.Id}/status", new { status = "PUBLISHED" });

        var quest = await CreateQuestAsync(admin, file.Id, "CH-I-QUEST-001", "BOTH", "MAIN");
        await SendAsync<QuestBody>(admin, HttpMethod.Post, $"/api/admin/quests/{quest.Id}/status", new { status = "PUBLISHED" });
        var task = await CreateTaskAsync(admin, quest.Id, "CH-I-TASK-001", "SHARED", "", ["REG-1026-17"], [locked.Id]);
        await SendAsync<TaskBody>(admin, HttpMethod.Post, $"/api/admin/quests/{quest.Id}/tasks/{task.Id}/status", new { status = "PUBLISHED" });

        using var juul = await _fixture.SignInAsync("juul", CaseApiFixture.ClientPassword);
        using var amber = await _fixture.SignInAsync("amber", CaseApiFixture.ClientPassword);
        var juulQuests = await juul.GetFromJsonAsync<ReadingSummary[]>( "/api/client/quests");
        var amberQuests = await amber.GetFromJsonAsync<ReadingSummary[]>("/api/client/quests");
        Assert.Contains(juulQuests!, item => item.Reference == "CH-I-QUEST-001");
        Assert.Contains(amberQuests!, item => item.Reference == "CH-I-QUEST-001");

        var before = await juul.GetStringAsync($"/api/client/quests/{quest.Id}");
        Assert.DoesNotContain("REG-1026-17", before, StringComparison.Ordinal);
        Assert.DoesNotContain("MAIN", before, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await juul.GetAsync($"/api/client/documents/{locked.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await amber.GetAsync($"/api/client/documents/{locked.Id}")).StatusCode);
        var visible = await juul.GetFromJsonAsync<PaperBody>($"/api/client/documents/{openPaper.Id}");
        Assert.Equal("CH-I-DOC-006", visible!.Reference);

        var refused = await SendAsync<AnswerBody>(juul, HttpMethod.Post, $"/api/client/quests/tasks/{task.Id}/answers", new { answer = "reg-1026-18" });
        Assert.Equal(HttpStatusCode.OK, refused.Status);
        Assert.False(refused.Body!.Accepted);
        Assert.False(refused.Body.Completed);
        Assert.Equal("The submitted reference could not be verified.", refused.Body.Message);
        Assert.DoesNotContain("REG-1026-17", refused.Raw, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, (await amber.GetAsync($"/api/client/documents/{locked.Id}")).StatusCode);

        var accepted = await SendAsync<AnswerBody>(amber, HttpMethod.Post, $"/api/client/quests/tasks/{task.Id}/answers", new { answer = "  reg-1026-17  " });
        Assert.Equal(HttpStatusCode.OK, accepted.Status);
        Assert.True(accepted.Body!.Accepted);
        Assert.True(accepted.Body.Completed);
        Assert.Equal("The registry entry has been identified. The associated record is now available for examination.", accepted.Body.Message);
        Assert.DoesNotContain("\"answers\"", accepted.Raw, StringComparison.Ordinal);

        var juulAfter = await juul.GetFromJsonAsync<ReadingQuest>($"/api/client/quests/{quest.Id}");
        var amberAfter = await amber.GetFromJsonAsync<ReadingQuest>($"/api/client/quests/{quest.Id}");
        Assert.True(juulAfter!.Tasks.Single().Completed);
        Assert.True(amberAfter!.Tasks.Single().Completed);
        Assert.Equal(1, juulAfter.SharedCompleted);
        Assert.Equal(1, amberAfter.SharedCompleted);
        Assert.Equal(0, juulAfter.SharedOpen);
        Assert.Contains("now available for examination", juulAfter.Tasks.Single().SuccessMessage, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, (await juul.GetAsync($"/api/client/documents/{locked.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await amber.GetAsync($"/api/client/documents/{locked.Id}")).StatusCode);

        var again = await SendAsync<AnswerBody>(juul, HttpMethod.Post, $"/api/client/quests/tasks/{task.Id}/answers", new { answer = "REG-1026-17" });
        Assert.Equal(HttpStatusCode.Conflict, again.Status);

        var submissions = await admin.GetFromJsonAsync<SubmissionBody[]>($"/api/admin/quests/{quest.Id}/submissions");
        Assert.Contains(submissions!, item => item.SubmittedBy == "Juul-V" && item.Outcome == "REFUSED" && item.Answer == "reg-1026-18" && item.Scope == "SHARED");
        Assert.Contains(submissions!, item => item.SubmittedBy == "Amber-D" && item.Outcome == "ACCEPTED" && item.Scope == "SHARED");
        var adminQuest = await admin.GetFromJsonAsync<QuestBody>($"/api/admin/quests/{quest.Id}");
        Assert.Contains("REG-1026-17", adminQuest!.Tasks.Single().Answers);
        Assert.True(adminQuest.Tasks.Single().Unlocks.Single().Released);
        Assert.Equal(HttpStatusCode.Forbidden, (await juul.GetAsync("/api/admin/quests")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PostAsJsonAsync($"/api/client/quests/tasks/{task.Id}/answers", new { answer = "REG-1026-17" })).StatusCode);
    }

    [Fact]
    public async Task An_individual_assignment_stays_with_its_participant()
    {
        using var admin = await _fixture.SignInAsync("administrator", CaseApiFixture.AdminPassword);
        var file = await CreateCaseAsync(admin, "PRIVATE-QUEST-001", "BOTH");
        var paper = await CreateDocumentAsync(admin, file.Id, "PRIVATE-DOC-001", "BOTH");
        await SendAsync<DocumentBody>(admin, HttpMethod.Post, $"/api/admin/cases/{file.Id}/documents/{paper.Id}/status", new { status = "PUBLISHED" });
        await SendAsync<CaseBody>(admin, HttpMethod.Post, $"/api/admin/cases/{file.Id}/status", new { status = "PUBLISHED" });
        var quest = await CreateQuestAsync(admin, file.Id, "PRIVATE-QUEST-A", "BOTH", "MAIN");
        await SendAsync<QuestBody>(admin, HttpMethod.Post, $"/api/admin/quests/{quest.Id}/status", new { status = "PUBLISHED" });
        var juulTask = await CreateTaskAsync(admin, quest.Id, "PRIVATE-TASK-JUUL", "INDIVIDUAL", "JUUL", ["JUUL-CODE"], [paper.Id]);
        var amberTask = await CreateTaskAsync(admin, quest.Id, "PRIVATE-TASK-AMBER", "INDIVIDUAL", "AMBER", ["AMBER-CODE"], []);
        await SendAsync<TaskBody>(admin, HttpMethod.Post, $"/api/admin/quests/{quest.Id}/tasks/{juulTask.Id}/status", new { status = "PUBLISHED" });
        await SendAsync<TaskBody>(admin, HttpMethod.Post, $"/api/admin/quests/{quest.Id}/tasks/{amberTask.Id}/status", new { status = "PUBLISHED" });

        using var juul = await _fixture.SignInAsync("juul", CaseApiFixture.ClientPassword);
        using var amber = await _fixture.SignInAsync("amber", CaseApiFixture.ClientPassword);
        Assert.Equal(HttpStatusCode.NotFound, (await juul.GetAsync($"/api/client/documents/{paper.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await amber.GetAsync($"/api/client/documents/{paper.Id}")).StatusCode);

        var juulQuest = await juul.GetFromJsonAsync<ReadingQuest>($"/api/client/quests/{quest.Id}");
        var amberQuest = await amber.GetFromJsonAsync<ReadingQuest>($"/api/client/quests/{quest.Id}");
        Assert.Equal(["PRIVATE-TASK-JUUL"], juulQuest!.Tasks.Select(item => item.Reference).ToArray());
        Assert.Equal(["PRIVATE-TASK-AMBER"], amberQuest!.Tasks.Select(item => item.Reference).ToArray());
        var crossed = await SendAsync<AnswerBody>(amber, HttpMethod.Post, $"/api/client/quests/tasks/{juulTask.Id}/answers", new { answer = "JUUL-CODE" });
        Assert.Equal(HttpStatusCode.NotFound, crossed.Status);

        var solved = await SendAsync<AnswerBody>(juul, HttpMethod.Post, $"/api/client/quests/tasks/{juulTask.Id}/answers", new { answer = "juul-code" });
        Assert.True(solved.Body!.Accepted);
        Assert.Equal(HttpStatusCode.OK, (await juul.GetAsync($"/api/client/documents/{paper.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await amber.GetAsync($"/api/client/documents/{paper.Id}")).StatusCode);
        var amberStill = await amber.GetFromJsonAsync<ReadingQuest>($"/api/client/quests/{quest.Id}");
        Assert.False(amberStill!.Tasks.Single().Completed);
    }

    [Fact]
    public async Task Replacing_the_accepted_answer_retires_the_previous_one()
    {
        using var admin = await _fixture.SignInAsync("administrator", CaseApiFixture.AdminPassword);
        var file = await CreateCaseAsync(admin, "REVISED-QUEST-001", "JUUL");
        await SendAsync<CaseBody>(admin, HttpMethod.Post, $"/api/admin/cases/{file.Id}/status", new { status = "PUBLISHED" });
        var quest = await CreateQuestAsync(admin, file.Id, "REVISED-QUEST-A", "JUUL", "SIDE");
        await SendAsync<QuestBody>(admin, HttpMethod.Post, $"/api/admin/quests/{quest.Id}/status", new { status = "PUBLISHED" });
        var task = await CreateTaskAsync(admin, quest.Id, "REVISED-TASK-001", "INDIVIDUAL", "JUUL", ["ONE"], []);
        await SendAsync<TaskBody>(admin, HttpMethod.Post, $"/api/admin/quests/{quest.Id}/tasks/{task.Id}/status", new { status = "PUBLISHED" });
        var revised = await SendAsync<TaskBody>(admin, HttpMethod.Put, $"/api/admin/quests/{quest.Id}/tasks/{task.Id}", TaskPayload("REVISED-TASK-001", "INDIVIDUAL", "JUUL", ["THREE"], []));
        Assert.Equal(HttpStatusCode.OK, revised.Status);
        Assert.Equal(["THREE"], revised.Body!.Answers);

        using var juul = await _fixture.SignInAsync("juul", CaseApiFixture.ClientPassword);
        var stale = await SendAsync<AnswerBody>(juul, HttpMethod.Post, $"/api/client/quests/tasks/{task.Id}/answers", new { answer = "ONE" });
        Assert.False(stale.Body!.Accepted);
        var current = await SendAsync<AnswerBody>(juul, HttpMethod.Post, $"/api/client/quests/tasks/{task.Id}/answers", new { answer = "three" });
        Assert.True(current.Body!.Accepted);
    }

    private static async Task<CaseBody> CreateCaseAsync(HttpClient admin, string reference, string audience)
    {
        var created = await SendAsync<CaseBody>(admin, HttpMethod.Post, "/api/admin/cases", new
        {
            reference,
            title = "A case",
            summary = "Held with the house.",
            introduction = "Read this first.",
            audience,
        });
        Assert.Equal(HttpStatusCode.Created, created.Status);
        return created.Body!;
    }

    private static async Task<DocumentBody> CreateDocumentAsync(HttpClient admin, Guid caseId, string reference, string audience)
    {
        var created = await SendAsync<DocumentBody>(admin, HttpMethod.Post, $"/api/admin/cases/{caseId}/documents", new
        {
            reference,
            title = "A paper",
            category = "Record",
            description = "",
            content = "The supporting record.",
            audience,
        });
        Assert.Equal(HttpStatusCode.Created, created.Status);
        return created.Body!;
    }

    private static async Task<QuestBody> CreateQuestAsync(HttpClient admin, Guid caseId, string reference, string audience, string kind)
    {
        var created = await SendAsync<QuestBody>(admin, HttpMethod.Post, "/api/admin/quests", new
        {
            reference,
            title = "The Missing Record",
            description = "An inconsistency has been identified in the records relating to the restricted allocation.",
            instructions = "Use the information available in your personal account documents and the October 2026 Closing Register.",
            caseId,
            audience,
            kind,
        });
        Assert.Equal(HttpStatusCode.Created, created.Status);
        return created.Body!;
    }

    private static async Task<TaskBody> CreateTaskAsync(HttpClient admin, Guid questId, string reference, string scope, string participant, string[] answers, Guid[] unlocks)
    {
        var created = await SendAsync<TaskBody>(admin, HttpMethod.Post, $"/api/admin/quests/{questId}/tasks", TaskPayload(reference, scope, participant, answers, unlocks));
        Assert.Equal(HttpStatusCode.Created, created.Status);
        return created.Body!;
    }

    private static object TaskPayload(string reference, string scope, string participant, string[] answers, Guid[] unlocks)
    {
        return new
        {
            reference,
            title = "Identify the Registry Entry",
            instructions = "Submit the reference number of the registry entry.",
            taskType = "TEXT",
            scope,
            participant,
            answers,
            ignoreCase = true,
            trimWhitespace = true,
            successMessage = "The registry entry has been identified. The associated record is now available for examination.",
            unlockDocumentIds = unlocks,
        };
    }

    private static async Task<(HttpStatusCode Status, T? Body, string Raw)> SendAsync<T>(HttpClient client, HttpMethod method, string path, object payload)
    {
        var token = await client.GetFromJsonAsync<CsrfResponse>("/api/auth/csrf");
        var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(payload) };
        request.Headers.TryAddWithoutValidation("Origin", "http://localhost:5173");
        request.Headers.TryAddWithoutValidation("X-CSRF-TOKEN", token?.Token);
        var response = await client.SendAsync(request);
        var raw = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            return (response.StatusCode, default, raw);
        }

        var body = JsonSerializer.Deserialize<T>(raw, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return (response.StatusCode, body, raw);
    }

    private sealed record CsrfResponse(string? Token);

    private sealed record CaseBody(Guid Id);

    private sealed record DocumentBody(Guid Id, string Reference);

    private sealed record QuestBody(Guid Id, string Reference, TaskBody[] Tasks);

    private sealed record TaskBody(Guid Id, string Reference, string[] Answers, UnlockBody[] Unlocks);

    private sealed record UnlockBody(Guid DocumentId, bool Released);

    private sealed record ReadingSummary(Guid Id, string Reference);

    private sealed record ReadingQuest(Guid Id, int SharedCompleted, int SharedOpen, ReadingTask[] Tasks);

    private sealed record ReadingTask(Guid Id, string Reference, bool Completed, string? SuccessMessage);

    private sealed record AnswerBody(bool Accepted, bool Completed, string Message);

    private sealed record SubmissionBody(string SubmittedBy, string Answer, string Outcome, string Scope);

    private sealed record PaperBody(string Reference);
}
