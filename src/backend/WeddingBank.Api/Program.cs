using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using WeddingBank.Api;
using WeddingBank.Api.Enquiries;
using WeddingBank.Api.Game;
using WeddingBank.Api.Identity;
using WeddingBank.Application.Game;
using WeddingBank.Infrastructure.Enquiries;
using WeddingBank.Infrastructure.Game;
using WeddingBank.Infrastructure.Identity;
using WeddingBank.Infrastructure.Persistence;

if (ChapterOneCli.Requested(args))
{
    Environment.Exit(await ChapterOneCli.RunAsync(args));
}

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
builder.Services.AddGame();
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

if (app.Environment.IsProduction())
{
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<WeddingBankDbContext>();
            await DatabaseConnection.ApplyAsync(db);
            await scope.ServiceProvider.GetRequiredService<HouseAccountBootstrap>().EnsureAsync();
            break;
        }
        catch (Exception) when (attempt < 15)
        {
            Console.WriteLine($"Database is not ready (attempt {attempt}).");
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}

if (!app.Configuration.GetValue("Documents:DeferReadBaseline", false))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<CaseService>().EstablishReadBaselineAsync();
}

app.UseForwardedHeaders();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapEnquiryEndpoints();
app.MapAuthEndpoints();
app.MapGameEndpoints();
app.MapHeistEndpoints();
app.MapCaseEndpoints();
app.MapQuestEndpoints();

app.Run();

public partial class Program;
