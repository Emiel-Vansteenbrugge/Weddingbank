using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using WeddingBank.Application.Identity;
using WeddingBank.Domain.Identity;

namespace WeddingBank.Api.Identity;

public static class HouseAuthenticationExtensions
{
    public const string SessionClaim = "sid";
    public const string LoginClaim = "login";

    public static IServiceCollection AddHouseAuthentication(this IServiceCollection services, bool isDevelopment)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "wb.session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = isDevelopment
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                options.Cookie.Path = "/";
                options.Cookie.IsEssential = true;
                options.SlidingExpiration = false;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.Events.OnRedirectToLogin = Reject(StatusCodes.Status401Unauthorized, "Sign in to continue.");
                options.Events.OnRedirectToAccessDenied = Reject(StatusCodes.Status403Forbidden, "You do not have access to this record.");
                options.Events.OnValidatePrincipal = ValidatePrincipalAsync;
            });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
            options.AddPolicy("Client", policy => policy.RequireRole(RoleNames.Client));
            options.AddPolicy("Admin", policy => policy.RequireRole(RoleNames.Admin));
        });

        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "wb.csrf";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = isDevelopment
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
            options.Cookie.Path = "/";
        });

        return services;
    }

    public static ClaimsPrincipal PrincipalFor(AuthUser user, Guid sessionId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(LoginClaim, user.Login),
            new(SessionClaim, sessionId.ToString()),
        };
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    private static async Task ValidatePrincipalAsync(CookieValidatePrincipalContext context)
    {
        var sessionValue = context.Principal?.FindFirstValue(SessionClaim);
        if (!Guid.TryParse(sessionValue, out var sessionId))
        {
            context.RejectPrincipal();
            return;
        }

        var service = context.HttpContext.RequestServices.GetRequiredService<WeddingBank.Application.Identity.AuthenticationService>();
        var user = await service.ValidateSessionAsync(sessionId, context.HttpContext.RequestAborted);
        if (user is null)
        {
            context.RejectPrincipal();
            return;
        }

        context.ReplacePrincipal(PrincipalFor(user, sessionId));
    }

    private static Func<RedirectContext<CookieAuthenticationOptions>, Task> Reject(int statusCode, string title)
    {
        return async context =>
        {
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(new { title });
        };
    }
}
