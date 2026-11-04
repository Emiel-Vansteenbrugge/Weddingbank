using System.Security.Claims;
using WeddingBank.Api.Identity;
using WeddingBank.Application.Game;
using WeddingBank.Domain.Game;

namespace WeddingBank.Api.Game;

internal static class GameEndpoints
{
    public static void MapGameEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/api/admin/game").RequireAuthorization("Admin");
        admin.MapGet("", OverviewAsync);
        admin.MapGet("/participants/{login}", ParticipantAsync);

        app.MapGet("/api/client/desk", DeskAsync).RequireAuthorization("Client");
    }

    private static async Task<IResult> OverviewAsync(GameService games, CancellationToken cancellationToken)
    {
        return Results.Ok(await games.OverviewAsync(cancellationToken));
    }

    private static async Task<IResult> ParticipantAsync(string login, GameService games, CancellationToken cancellationToken)
    {
        var desk = await games.DeskAsync(login, cancellationToken);
        return desk is null
            ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "That participant is not part of the game.")
            : Results.Ok(desk);
    }

    private static async Task<IResult> DeskAsync(ClaimsPrincipal principal, GameService games, CancellationToken cancellationToken)
    {
        var login = principal.FindFirstValue(HouseAuthenticationExtensions.LoginClaim);
        if (login is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Sign in to continue.");
        }

        var desk = await games.DeskAsync(login, cancellationToken);
        return desk is null
            ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "This account is not a participant.")
            : Results.Ok(desk);
    }
}
