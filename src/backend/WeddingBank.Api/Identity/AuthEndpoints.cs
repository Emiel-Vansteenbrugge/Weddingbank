using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using WeddingBank.Application;
using WeddingBank.Application.Identity;
using HouseSignIn = WeddingBank.Application.Identity.AuthenticationService;

namespace WeddingBank.Api.Identity;

internal static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapGet("/api/auth/csrf", (HttpContext http, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(http);
            return Results.Ok(new { token = tokens.RequestToken });
        }).AllowAnonymous();

        app.MapPost("/api/auth/login", SignInAsync)
            .AllowAnonymous()
            .RequireRateLimiting("login")
            .AddEndpointFilter(HouseRequestGuard.Invoke);
        app.MapPost("/api/auth/logout", SignOutAsync)
            .AllowAnonymous()
            .AddEndpointFilter(HouseRequestGuard.Invoke);
        app.MapGet("/api/auth/me", MeAsync).RequireAuthorization();

        app.MapGet("/api/admin/audit", AuditAsync).RequireAuthorization("Admin");
        app.MapGet("/api/admin/house-accounts", ListHouseAccountsAsync).RequireAuthorization("Admin");
        app.MapPost("/api/admin/house-accounts/{login}/password", ChangeHousePasswordAsync)
            .RequireAuthorization("Admin")
            .AddEndpointFilter(HouseRequestGuard.Invoke);
    }

    private static async Task<IResult> SignInAsync(
        LoginRequest request,
        HttpContext http,
        HouseSignIn authentication,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["login"] = ["Enter your sign-in details."],
            });
        }

        var signedIn = await authentication.SignInAsync(request.Login, request.Password, HouseRequestGuard.ClientIp(http), cancellationToken);
        if (signedIn is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "The sign-in details are not recognised.");
        }

        await http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            HouseAuthenticationExtensions.PrincipalFor(signedIn.User, signedIn.SessionId),
            new AuthenticationProperties
            {
                IsPersistent = true,
                IssuedUtc = signedIn.IssuedAt,
                ExpiresUtc = signedIn.ExpiresAt,
                AllowRefresh = false,
            });
        return Results.Ok(signedIn.User);
    }

    private static async Task<IResult> SignOutAsync(
        HttpContext http,
        HouseSignIn authentication,
        CancellationToken cancellationToken)
    {
        var sessionValue = http.User.FindFirstValue(HouseAuthenticationExtensions.SessionClaim);
        Guid? sessionId = Guid.TryParse(sessionValue, out var parsed) ? parsed : null;
        await authentication.SignOutAsync(sessionId, HouseRequestGuard.ClientIp(http), cancellationToken);
        await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.NoContent();
    }

    private static IResult MeAsync(ClaimsPrincipal principal)
    {
        var idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(idValue, out var id))
        {
            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Sign in to continue.");
        }

        return Results.Ok(new AuthUser(
            id,
            principal.FindFirstValue(HouseAuthenticationExtensions.LoginClaim) ?? "",
            principal.Identity?.Name ?? "",
            principal.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray()));
    }

    private static async Task<IResult> AuditAsync(
        ClaimsPrincipal principal,
        HttpContext http,
        HouseSignIn authentication,
        CancellationToken cancellationToken)
    {
        var idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(idValue, out var id))
        {
            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Sign in to continue.");
        }

        var entries = await authentication.ReadAuditAsync(id, HouseRequestGuard.ClientIp(http), cancellationToken);
        return Results.Ok(entries);
    }

    private static async Task<IResult> ListHouseAccountsAsync(HouseAccountService accounts, CancellationToken cancellationToken)
    {
        return Results.Ok(await accounts.ListAsync(cancellationToken));
    }

    private static async Task<IResult> ChangeHousePasswordAsync(
        string login,
        PasswordChangeRequest request,
        ClaimsPrincipal principal,
        HttpContext http,
        HouseAccountService accounts,
        CancellationToken cancellationToken)
    {
        var idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(idValue, out var actorId))
        {
            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Sign in to continue.");
        }

        var result = await accounts.ChangePasswordAsync(
            login,
            request.Password,
            request.Confirmation,
            actorId,
            HouseRequestGuard.ClientIp(http),
            cancellationToken);
        return result.Status switch
        {
            OperationStatus.Ok => Results.NoContent(),
            OperationStatus.NotFound => Results.Problem(statusCode: StatusCodes.Status404NotFound, title: result.Title),
            OperationStatus.Invalid => Results.ValidationProblem(new Dictionary<string, string[]>(result.Errors!)),
            _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError, title: "The request could not be completed."),
        };
    }

}

internal sealed class LoginRequest
{
    public string? Login { get; init; }

    public string? Password { get; init; }
}

internal sealed class PasswordChangeRequest
{
    public string? Password { get; init; }

    public string? Confirmation { get; init; }
}
