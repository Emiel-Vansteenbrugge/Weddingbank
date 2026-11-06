using System.Security.Claims;
using WeddingBank.Api.Identity;
using WeddingBank.Application;
using WeddingBank.Application.Game;
using WeddingBank.Application.Identity;

namespace WeddingBank.Api.Game;

internal static class QuestEndpoints
{
    public static void MapQuestEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/api/admin/quests").RequireAuthorization("Admin");
        admin.MapGet("", ListAsync);
        admin.MapPost("", CreateAsync).AddEndpointFilter(HouseRequestGuard.Invoke);
        admin.MapGet("/{id:guid}", ReadAsync);
        admin.MapPut("/{id:guid}", UpdateAsync).AddEndpointFilter(HouseRequestGuard.Invoke);
        admin.MapPost("/{id:guid}/status", SetStatusAsync).AddEndpointFilter(HouseRequestGuard.Invoke);
        admin.MapPost("/{id:guid}/tasks", AddTaskAsync).AddEndpointFilter(HouseRequestGuard.Invoke);
        admin.MapPut("/{id:guid}/tasks/{taskId:guid}", UpdateTaskAsync).AddEndpointFilter(HouseRequestGuard.Invoke);
        admin.MapPost("/{id:guid}/tasks/{taskId:guid}/status", SetTaskStatusAsync).AddEndpointFilter(HouseRequestGuard.Invoke);
        admin.MapGet("/{id:guid}/submissions", SubmissionsAsync);

        var client = app.MapGroup("/api/client/quests").RequireAuthorization("Client");
        client.MapGet("", ListForParticipantAsync);
        client.MapGet("/{id:guid}", ReadForParticipantAsync);
        client.MapPost("/tasks/{taskId:guid}/answers", SubmitAsync).AddEndpointFilter(HouseRequestGuard.Invoke);
    }

    private static async Task<IResult> ListAsync(QuestService quests, CancellationToken cancellationToken)
    {
        return Results.Ok(await quests.ListAsync(cancellationToken));
    }

    private static async Task<IResult> CreateAsync(QuestWriteRequest request, QuestService quests, CancellationToken cancellationToken)
    {
        var result = await quests.CreateAsync(Draft(request), cancellationToken);
        return result.Status == OperationStatus.Ok
            ? Results.Created($"/api/admin/quests/{result.Value!.Id}", result.Value)
            : From(result);
    }

    private static async Task<IResult> ReadAsync(Guid id, QuestService quests, CancellationToken cancellationToken)
    {
        return From(await quests.ReadAsync(id, cancellationToken));
    }

    private static async Task<IResult> UpdateAsync(Guid id, QuestWriteRequest request, QuestService quests, CancellationToken cancellationToken)
    {
        return From(await quests.UpdateAsync(id, Draft(request), cancellationToken));
    }

    private static async Task<IResult> SetStatusAsync(Guid id, StatusRequest request, QuestService quests, CancellationToken cancellationToken)
    {
        return From(await quests.SetStatusAsync(id, request.Status, cancellationToken));
    }

    private static async Task<IResult> AddTaskAsync(Guid id, TaskWriteRequest request, QuestService quests, CancellationToken cancellationToken)
    {
        var result = await quests.AddTaskAsync(id, Draft(request), cancellationToken);
        return result.Status == OperationStatus.Ok
            ? Results.Created($"/api/admin/quests/{id}/tasks/{result.Value!.Id}", result.Value)
            : From(result);
    }

    private static async Task<IResult> UpdateTaskAsync(Guid id, Guid taskId, TaskWriteRequest request, QuestService quests, CancellationToken cancellationToken)
    {
        return From(await quests.UpdateTaskAsync(id, taskId, Draft(request), cancellationToken));
    }

    private static async Task<IResult> SetTaskStatusAsync(Guid id, Guid taskId, StatusRequest request, QuestService quests, CancellationToken cancellationToken)
    {
        return From(await quests.SetTaskStatusAsync(id, taskId, request.Status, cancellationToken));
    }

    private static async Task<IResult> SubmissionsAsync(Guid id, QuestService quests, CancellationToken cancellationToken)
    {
        var quest = await quests.ReadAsync(id, cancellationToken);
        return quest.Status == OperationStatus.NotFound
            ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: quest.Title)
            : Results.Ok(await quests.SubmissionsAsync(id, cancellationToken));
    }

    private static async Task<IResult> ListForParticipantAsync(ClaimsPrincipal principal, QuestService quests, CancellationToken cancellationToken)
    {
        var login = ParticipantLogin(principal);
        return login is null
            ? Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Sign in to continue.")
            : Results.Ok(await quests.ListForAsync(login, cancellationToken));
    }

    private static async Task<IResult> ReadForParticipantAsync(Guid id, ClaimsPrincipal principal, QuestService quests, CancellationToken cancellationToken)
    {
        var login = ParticipantLogin(principal);
        return login is null
            ? Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Sign in to continue.")
            : From(await quests.ReadForAsync(login, id, cancellationToken));
    }

    private static async Task<IResult> SubmitAsync(Guid taskId, AnswerRequest request, ClaimsPrincipal principal, QuestService quests, CancellationToken cancellationToken)
    {
        var login = ParticipantLogin(principal);
        return login is null
            ? Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Sign in to continue.")
            : From(await quests.SubmitAsync(login, taskId, request.Answer, cancellationToken));
    }

    private static string? ParticipantLogin(ClaimsPrincipal principal)
    {
        return LoginName.Normalize(principal.FindFirstValue(HouseAuthenticationExtensions.LoginClaim));
    }

    private static QuestDraft Draft(QuestWriteRequest request)
    {
        return new QuestDraft(request.Reference, request.Title, request.Description, request.Instructions, request.CaseId, request.Audience, request.Kind);
    }

    private static TaskDraft Draft(TaskWriteRequest request)
    {
        return new TaskDraft(request.Reference, request.Title, request.Instructions, request.TaskType, request.Scope, request.Participant, request.Answers, request.IgnoreCase, request.TrimWhitespace, request.SuccessMessage, request.UnlockDocumentIds);
    }

    private static IResult From<T>(OperationResult<T> result)
        where T : class
    {
        return result.Status switch
        {
            OperationStatus.Ok => Results.Ok(result.Value),
            OperationStatus.NotFound => Results.Problem(statusCode: StatusCodes.Status404NotFound, title: result.Title),
            OperationStatus.Conflict => Results.Problem(statusCode: StatusCodes.Status409Conflict, title: result.Title),
            OperationStatus.Invalid => Results.ValidationProblem(new Dictionary<string, string[]>(result.Errors!)),
            _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError, title: "The request could not be completed."),
        };
    }
}

internal sealed class QuestWriteRequest
{
    public string? Reference { get; init; }

    public string? Title { get; init; }

    public string? Description { get; init; }

    public string? Instructions { get; init; }

    public Guid? CaseId { get; init; }

    public string? Audience { get; init; }

    public string? Kind { get; init; }
}

internal sealed class TaskWriteRequest
{
    public string? Reference { get; init; }

    public string? Title { get; init; }

    public string? Instructions { get; init; }

    public string? TaskType { get; init; }

    public string? Scope { get; init; }

    public string? Participant { get; init; }

    public List<string>? Answers { get; init; }

    public bool? IgnoreCase { get; init; }

    public bool? TrimWhitespace { get; init; }

    public string? SuccessMessage { get; init; }

    public List<Guid>? UnlockDocumentIds { get; init; }
}

internal sealed class AnswerRequest
{
    public string? Answer { get; init; }
}
