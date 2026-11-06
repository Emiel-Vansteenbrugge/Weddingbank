using System.Security.Claims;
using WeddingBank.Api.Identity;
using WeddingBank.Application;
using WeddingBank.Application.Game;
using WeddingBank.Application.Identity;

namespace WeddingBank.Api.Game;

internal static class CaseEndpoints
{
    public static void MapCaseEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/api/admin/cases").RequireAuthorization("Admin");
        admin.MapGet("", ListAsync);
        admin.MapPost("", CreateAsync).AddEndpointFilter(HouseRequestGuard.Invoke);
        admin.MapPost("/order", ReorderAsync).AddEndpointFilter(HouseRequestGuard.Invoke);
        admin.MapGet("/{id:guid}", ReadAsync);
        admin.MapPut("/{id:guid}", UpdateAsync).AddEndpointFilter(HouseRequestGuard.Invoke);
        admin.MapPost("/{id:guid}/status", SetStatusAsync).AddEndpointFilter(HouseRequestGuard.Invoke);
        admin.MapPost("/{id:guid}/documents", AddDocumentAsync).AddEndpointFilter(HouseRequestGuard.Invoke);
        admin.MapPost("/{id:guid}/documents/order", ReorderDocumentsAsync).AddEndpointFilter(HouseRequestGuard.Invoke);
        admin.MapGet("/{id:guid}/documents/{documentId:guid}", ReadDocumentAsync);
        admin.MapPut("/{id:guid}/documents/{documentId:guid}", UpdateDocumentAsync).AddEndpointFilter(HouseRequestGuard.Invoke);
        admin.MapPost("/{id:guid}/documents/{documentId:guid}/status", SetDocumentStatusAsync).AddEndpointFilter(HouseRequestGuard.Invoke);

        var client = app.MapGroup("/api/client").RequireAuthorization("Client");
        client.MapGet("/cases", ListForParticipantAsync);
        client.MapGet("/cases/{id:guid}", ReadForParticipantAsync);
        client.MapGet("/documents", PapersAsync);
        client.MapGet("/documents/{id:guid}", ReadPaperAsync);
    }

    private static async Task<IResult> ListAsync(CaseService cases, CancellationToken cancellationToken)
    {
        return Results.Ok(await cases.ListAsync(cancellationToken));
    }

    private static async Task<IResult> CreateAsync(CaseWriteRequest request, CaseService cases, CancellationToken cancellationToken)
    {
        var result = await cases.CreateAsync(Draft(request), cancellationToken);
        return result.Status == OperationStatus.Ok
            ? Results.Created($"/api/admin/cases/{result.Value!.Id}", result.Value)
            : From(result);
    }

    private static async Task<IResult> ReadAsync(Guid id, CaseService cases, CancellationToken cancellationToken)
    {
        return From(await cases.ReadAsync(id, cancellationToken));
    }

    private static async Task<IResult> UpdateAsync(Guid id, CaseWriteRequest request, CaseService cases, CancellationToken cancellationToken)
    {
        return From(await cases.UpdateAsync(id, Draft(request), cancellationToken));
    }

    private static async Task<IResult> SetStatusAsync(Guid id, StatusRequest request, CaseService cases, CancellationToken cancellationToken)
    {
        return From(await cases.SetStatusAsync(id, request.Status, cancellationToken));
    }

    private static async Task<IResult> ReorderAsync(OrderRequest request, CaseService cases, CancellationToken cancellationToken)
    {
        return From(await cases.ReorderAsync(request.Ids, cancellationToken));
    }

    private static async Task<IResult> AddDocumentAsync(Guid id, DocumentWriteRequest request, CaseService cases, CancellationToken cancellationToken)
    {
        var result = await cases.AddDocumentAsync(id, Draft(request), cancellationToken);
        return result.Status == OperationStatus.Ok
            ? Results.Created($"/api/admin/cases/{id}/documents/{result.Value!.Id}", result.Value)
            : From(result);
    }

    private static async Task<IResult> ReadDocumentAsync(Guid id, Guid documentId, CaseService cases, CancellationToken cancellationToken)
    {
        return From(await cases.ReadDocumentAsync(id, documentId, cancellationToken));
    }

    private static async Task<IResult> UpdateDocumentAsync(Guid id, Guid documentId, DocumentWriteRequest request, CaseService cases, CancellationToken cancellationToken)
    {
        return From(await cases.UpdateDocumentAsync(id, documentId, Draft(request), cancellationToken));
    }

    private static async Task<IResult> SetDocumentStatusAsync(Guid id, Guid documentId, StatusRequest request, CaseService cases, CancellationToken cancellationToken)
    {
        return From(await cases.SetDocumentStatusAsync(id, documentId, request.Status, cancellationToken));
    }

    private static async Task<IResult> ReorderDocumentsAsync(Guid id, OrderRequest request, CaseService cases, CancellationToken cancellationToken)
    {
        return From(await cases.ReorderDocumentsAsync(id, request.Ids, cancellationToken));
    }

    private static async Task<IResult> ListForParticipantAsync(ClaimsPrincipal principal, CaseService cases, CancellationToken cancellationToken)
    {
        var login = ParticipantLogin(principal);
        return login is null
            ? Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Sign in to continue.")
            : Results.Ok(await cases.ListForAsync(login, cancellationToken));
    }

    private static async Task<IResult> ReadForParticipantAsync(Guid id, ClaimsPrincipal principal, CaseService cases, CancellationToken cancellationToken)
    {
        var login = ParticipantLogin(principal);
        return login is null
            ? Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Sign in to continue.")
            : From(await cases.ReadForAsync(login, id, cancellationToken));
    }

    private static async Task<IResult> PapersAsync(ClaimsPrincipal principal, CaseService cases, CancellationToken cancellationToken)
    {
        var login = ParticipantLogin(principal);
        return login is null
            ? Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Sign in to continue.")
            : Results.Ok(await cases.PapersForAsync(login, cancellationToken));
    }

    private static async Task<IResult> ReadPaperAsync(Guid id, ClaimsPrincipal principal, CaseService cases, CancellationToken cancellationToken)
    {
        var login = ParticipantLogin(principal);
        return login is null
            ? Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Sign in to continue.")
            : From(await cases.ReadPaperAsync(login, id, cancellationToken));
    }

    private static string? ParticipantLogin(ClaimsPrincipal principal)
    {
        return LoginName.Normalize(principal.FindFirstValue(HouseAuthenticationExtensions.LoginClaim));
    }

    private static CaseDraft Draft(CaseWriteRequest request)
    {
        return new CaseDraft(request.Reference, request.Title, request.Summary, request.Introduction, request.Audience);
    }

    private static DocumentDraft Draft(DocumentWriteRequest request)
    {
        return new DocumentDraft(request.Reference, request.Title, request.Category, request.Description, request.Content, request.Audience);
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

internal sealed class CaseWriteRequest
{
    public string? Reference { get; init; }

    public string? Title { get; init; }

    public string? Summary { get; init; }

    public string? Introduction { get; init; }

    public string? Audience { get; init; }
}

internal sealed class DocumentWriteRequest
{
    public string? Reference { get; init; }

    public string? Title { get; init; }

    public string? Category { get; init; }

    public string? Description { get; init; }

    public string? Content { get; init; }

    public string? Audience { get; init; }
}

internal sealed class StatusRequest
{
    public string? Status { get; init; }
}

internal sealed class OrderRequest
{
    public List<Guid>? Ids { get; init; }
}
