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

    internal static bool OriginAllowed(HttpRequest request, AuthOptions options)
    {
        if (!request.Headers.TryGetValue(Microsoft.Net.Http.Headers.HeaderNames.Origin, out var raw))
        {
            return false;
        }

        var origin = raw.ToString().Trim().TrimEnd('/');
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri) || originUri.Host.Length == 0)
        {
            return false;
        }

        if (originUri.Scheme is not ("https" or "http"))
        {
            return false;
        }

        foreach (var allowed in options.AllowedOrigins)
        {
            if (Uri.TryCreate(allowed.Trim().TrimEnd('/'), UriKind.Absolute, out var allowedUri)
                && SameSite(originUri, allowedUri))
            {
                return true;
            }
        }

        return HostsOf(request).Any(host => string.Equals(originUri.Authority, host, StringComparison.OrdinalIgnoreCase));
    }

    private static bool SameSite(Uri actual, Uri allowed)
    {
        if (!string.Equals(actual.Host, allowed.Host, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (actual.IsDefaultPort && allowed.IsDefaultPort)
        {
            return true;
        }

        return actual.Port == allowed.Port;
    }

    private static IEnumerable<string> HostsOf(HttpRequest request)
    {
        if (request.Host.HasValue)
        {
            yield return request.Host.Value;
        }

        if (!request.Headers.TryGetValue("X-Forwarded-Host", out var forwarded))
        {
            yield break;
        }

        var value = forwarded.ToString().Split(',')[0].Trim();
        if (value.Length > 0)
        {
            yield return value;
        }
    }
}
