using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using WeddingBank.Api.Enquiries;
using WeddingBank.Infrastructure.Enquiries;

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
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("enquiries", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 8,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
            }));
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseRateLimiter();
app.MapHealthChecks("/health");
app.MapEnquiryEndpoints();

app.Run();

public partial class Program;
