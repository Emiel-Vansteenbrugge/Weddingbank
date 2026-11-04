using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using WeddingBank.Api.Clients;
using WeddingBank.Api.Enquiries;
using WeddingBank.Api.Identity;
using WeddingBank.Infrastructure.Clients;
using WeddingBank.Infrastructure.Enquiries;
using WeddingBank.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

var resendOverrides = new Dictionary<string, string?>();
var resendKey = Environment.GetEnvironmentVariable("RESEND_API_KEY");
if (!string.IsNullOrWhiteSpace(resendKey))
{
    resendOverrides["Resend:ApiKey"] = resendKey;
}

var resendFrom = Environment.GetEnvironmentVariable("RESEND_FROM_EMAIL");
if (!string.IsNullOrWhiteSpace(resendFrom))
{
    resendOverrides["Resend:FromEmail"] = resendFrom;
}

if (resendOverrides.Count > 0)
{
    builder.Configuration.AddInMemoryCollection(resendOverrides);
}

builder.Services.AddHealthChecks();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddResend(builder.Configuration);
builder.Services.AddWeddingBankIdentity(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddClientDirectory();
builder.Services.AddHouseAuthentication(builder.Environment.IsDevelopment());
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        if (context.HttpContext.Request.Path.StartsWithSegments("/api/auth/login"))
        {
            await context.HttpContext.Response.WriteAsJsonAsync(
                new { title = "Sign-in is temporarily unavailable. Please wait and try again." },
                token);
        }
    };
    options.AddPolicy("enquiries", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 8,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
            }));
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
            }));
});

var app = builder.Build();

if (args.Contains("--seed-development-users"))
{
    if (!app.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "Development users can only be created when ASPNETCORE_ENVIRONMENT is Development.");
    }

    await using var scope = app.Services.CreateAsyncScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DevelopmentUserSeeder>();
    await seeder.SeedAsync();
    return;
}

// Migrations are applied by hand. Do not call Database.Migrate() here.
app.UseForwardedHeaders();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapEnquiryEndpoints();
app.MapAuthEndpoints();
app.MapClientEndpoints();

app.Run();

public partial class Program;
