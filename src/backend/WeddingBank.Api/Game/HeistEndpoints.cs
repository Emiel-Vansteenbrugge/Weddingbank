using WeddingBank.Api.Identity;
using WeddingBank.Application.Game;

namespace WeddingBank.Api.Game;

internal static class HeistEndpoints
{
    public static void MapHeistEndpoints(this WebApplication app)
    {
        app.MapGet("/api/heist", ReadAsync).AllowAnonymous();

        var admin = app.MapGroup("/api/admin/heist").RequireAuthorization("Admin");
        admin.MapPost("/open", OpenAsync).AddEndpointFilter(HouseRequestGuard.Invoke);
        admin.MapPost("/close", CloseAsync).AddEndpointFilter(HouseRequestGuard.Invoke);
    }

    private static async Task<IResult> ReadAsync(HeistService heist, CancellationToken cancellationToken)
    {
        return Results.Ok(await heist.ReadAsync(cancellationToken));
    }

    private static async Task<IResult> OpenAsync(HeistService heist, CancellationToken cancellationToken)
    {
        return Results.Ok(await heist.OpenAsync(cancellationToken));
    }

    private static async Task<IResult> CloseAsync(HeistService heist, CancellationToken cancellationToken)
    {
        return Results.Ok(await heist.CloseAsync(cancellationToken));
    }
}
