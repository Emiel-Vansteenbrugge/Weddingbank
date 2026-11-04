using Microsoft.Extensions.Options;
using WeddingBank.Infrastructure.Identity;

namespace WeddingBank.Api.Identity;

internal static class HouseRequestGuard
{
    public static async ValueTask<object?> Invoke(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var options = http.RequestServices.GetRequiredService<IOptions<AuthOptions>>().Value;
        if (!OriginAllowed(http.Request, options))
        {
            return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "The request origin is not allowed.");
        }

        try
        {
            var antiforgery = http.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>();
            await antiforgery.ValidateRequestAsync(http);
        }
        catch (Microsoft.AspNetCore.Antiforgery.AntiforgeryValidationException)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "The request could not be verified.");
        }

        return await next(context);
    }

    public static string? ClientIp(HttpContext http)
    {
        return http.Connection.RemoteIpAddress?.ToString();
    }

    private static bool OriginAllowed(HttpRequest request, AuthOptions options)
    {
        if (!request.Headers.TryGetValue(Microsoft.Net.Http.Headers.HeaderNames.Origin, out var origin))
        {
            return false;
        }

        return options.AllowedOrigins.Contains(origin.ToString(), StringComparer.Ordinal);
    }
}
