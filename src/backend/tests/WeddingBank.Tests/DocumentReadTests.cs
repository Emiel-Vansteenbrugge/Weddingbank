using System.Net;
using System.Net.Http.Json;

namespace WeddingBank.Tests;

public sealed class DocumentReadTests
{
    [Fact]
    public async Task A_new_or_unlocked_document_is_new_until_that_participant_opens_it()
    {
        using var fixture = new CaseApiFixture();
        await fixture.InitializeChapterOneAsync();
        await fixture.EstablishDocumentReadsAsync();
        using var admin = await fixture.SignInAsync("administrator", CaseApiFixture.AdminPassword);
        using var juul = await fixture.SignInAsync("juul", CaseApiFixture.ClientPassword);
        using var amber = await fixture.SignInAsync("amber", CaseApiFixture.ClientPassword);

        Assert.All(await Papers(juul), paper => Assert.False(paper.IsNew));
        Assert.All(await Papers(amber), paper => Assert.False(paper.IsNew));

        var file = (await admin.GetFromJsonAsync<CaseItem[]>("/api/admin/cases"))!.Single(item => item.Reference == "CH-I-001");
        var created = await SendAsync<DocumentItem>(admin, HttpMethod.Post, $"/api/admin/cases/{file.Id}/documents", new
        {
            reference = "CH-I-DOC-READ",
            title = "Supplementary Note",
            category = "Note",
            description = "",
            content = "A note added after read tracking began.",
            audience = "BOTH",
        });
        Assert.Equal(HttpStatusCode.Created, created.Status);
        await SendAsync<object>(admin, HttpMethod.Post, $"/api/admin/cases/{file.Id}/documents/{created.Body!.Id}/status", new { status = "PUBLISHED" });

        Assert.True((await Papers(juul)).Single(item => item.Reference == "CH-I-DOC-READ").IsNew);
        Assert.True((await Papers(amber)).Single(item => item.Reference == "CH-I-DOC-READ").IsNew);

        Assert.Equal(HttpStatusCode.OK, (await juul.GetAsync($"/api/client/documents/{created.Body.Id}")).StatusCode);
        Assert.False((await Papers(juul)).Single(item => item.Reference == "CH-I-DOC-READ").IsNew);
        Assert.True((await Papers(amber)).Single(item => item.Reference == "CH-I-DOC-READ").IsNew);
        Assert.Equal(HttpStatusCode.OK, (await amber.GetAsync($"/api/client/documents/{created.Body.Id}")).StatusCode);
        Assert.False((await Papers(amber)).Single(item => item.Reference == "CH-I-DOC-READ").IsNew);

        using var juulAgain = await fixture.SignInAsync("juul", CaseApiFixture.ClientPassword);
        Assert.False((await Papers(juulAgain)).Single(item => item.Reference == "CH-I-DOC-READ").IsNew);
        await SendAsync<object>(admin, HttpMethod.Post, $"/api/admin/cases/{file.Id}/documents/{created.Body.Id}/status", new { status = "DRAFT" });
        await SendAsync<object>(admin, HttpMethod.Post, $"/api/admin/cases/{file.Id}/documents/{created.Body.Id}/status", new { status = "PUBLISHED" });
        Assert.False((await Papers(juulAgain)).Single(item => item.Reference == "CH-I-DOC-READ").IsNew);
        Assert.False((await Papers(amber)).Single(item => item.Reference == "CH-I-DOC-READ").IsNew);

        var quests = (await admin.GetFromJsonAsync<QuestItem[]>("/api/admin/quests"))!;
        var first = await admin.GetFromJsonAsync<QuestDetail>($"/api/admin/quests/{quests.Single(item => item.Reference == "CH-I-QUEST-001").Id}");
        var second = await admin.GetFromJsonAsync<QuestDetail>($"/api/admin/quests/{quests.Single(item => item.Reference == "CH-I-QUEST-002").Id}");
        var solved = await SendAsync<AnswerItem>(amber, HttpMethod.Post, $"/api/client/quests/tasks/{first!.Tasks.Single().Id}/answers", new { answer = "REG-1026-17" });
        Assert.True(solved.Body!.Accepted);

        var juulUnlocked = await Papers(juul);
        var amberUnlocked = await Papers(amber);
        Assert.True(juulUnlocked.Single(item => item.Reference == "CH-I-DOC-007").IsNew);
        Assert.True(amberUnlocked.Single(item => item.Reference == "CH-I-DOC-007").IsNew);
        Assert.True(juulUnlocked.Single(item => item.Reference == "CH-I-DOC-010").IsNew);
        Assert.True(amberUnlocked.Single(item => item.Reference == "CH-I-DOC-010").IsNew);
        Assert.True(juulUnlocked.Single(item => item.Reference == "CH-I-DOC-008").IsNew);
        Assert.DoesNotContain(amberUnlocked, item => item.Reference == "CH-I-DOC-008");

        var detail = await admin.GetFromJsonAsync<CaseDetail>($"/api/admin/cases/{file.Id}");
        var hidden = detail!.Documents.Single(item => item.Reference == "CH-I-DOC-011");
        Assert.Equal(HttpStatusCode.NotFound, (await juul.GetAsync($"/api/client/documents/{hidden.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await amber.GetAsync($"/api/client/documents/{hidden.Id}")).StatusCode);

        var accepted = await SendAsync<AnswerItem>(juul, HttpMethod.Post, $"/api/client/quests/tasks/{second!.Tasks.Single().Id}/answers", new { answer = "Compliance" });
        Assert.True(accepted.Body!.Accepted);
        Assert.True((await Papers(juul)).Single(item => item.Reference == "CH-I-DOC-011").IsNew);
        Assert.True((await Papers(amber)).Single(item => item.Reference == "CH-I-DOC-011").IsNew);
        Assert.Equal(HttpStatusCode.OK, (await juul.GetAsync($"/api/client/documents/{hidden.Id}")).StatusCode);
        Assert.False((await Papers(juul)).Single(item => item.Reference == "CH-I-DOC-011").IsNew);
        Assert.True((await Papers(amber)).Single(item => item.Reference == "CH-I-DOC-011").IsNew);
    }

    private static async Task<ListedPaper[]> Papers(HttpClient client)
    {
        return (await client.GetFromJsonAsync<ListedPaper[]>("/api/client/documents"))!;
    }

    private static async Task<(HttpStatusCode Status, T? Body)> SendAsync<T>(HttpClient client, HttpMethod method, string path, object payload)
    {
        var token = await client.GetFromJsonAsync<TokenBody>("/api/auth/csrf");
        var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(payload) };
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

    private sealed record ListedPaper(string Reference, bool IsNew);

    private sealed record CaseItem(Guid Id, string Reference);

    private sealed record CaseDetail(DocumentItem[] Documents);

    private sealed record DocumentItem(Guid Id, string Reference);

    private sealed record QuestItem(Guid Id, string Reference);

    private sealed record QuestDetail(TaskItem[] Tasks);

    private sealed record TaskItem(Guid Id);

    private sealed record AnswerItem(bool Accepted);
}
