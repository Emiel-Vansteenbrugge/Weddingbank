using Microsoft.AspNetCore.Http;
using WeddingBank.Api.Identity;
using WeddingBank.Infrastructure.Identity;

namespace WeddingBank.Tests;

public sealed class HouseOriginTests
{
    [Fact]
    public void The_public_site_is_accepted_on_http_and_on_its_forwarded_host()
    {
        var options = new AuthOptions { AllowedOrigins = ["https://wedding-bank.evsb.io"] };

        var http = new DefaultHttpContext();
        http.Request.Headers.Origin = "http://wedding-bank.evsb.io";
        Assert.True(HouseRequestGuard.OriginAllowed(http.Request, options));

        var direct = new DefaultHttpContext();
        direct.Request.Headers.Origin = "http://10.120.10.100:8080";
        direct.Request.Host = new HostString("10.120.10.100", 8080);
        Assert.True(HouseRequestGuard.OriginAllowed(direct.Request, new AuthOptions()));

        var forwarded = new DefaultHttpContext();
        forwarded.Request.Headers.Origin = "https://wedding-bank.evsb.io";
        forwarded.Request.Host = new HostString("10.120.10.100", 8080);
        forwarded.Request.Headers["X-Forwarded-Host"] = "wedding-bank.evsb.io";
        Assert.True(HouseRequestGuard.OriginAllowed(forwarded.Request, new AuthOptions()));
    }

    [Fact]
    public void Another_site_is_refused_even_when_it_names_the_house_host()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers.Origin = "https://evil.example";
        http.Request.Host = new HostString("wedding-bank.evsb.io");
        http.Request.Headers["X-Forwarded-Host"] = "wedding-bank.evsb.io";

        Assert.False(HouseRequestGuard.OriginAllowed(http.Request, new AuthOptions
        {
            AllowedOrigins = ["https://wedding-bank.evsb.io"],
        }));
        Assert.False(HouseRequestGuard.OriginAllowed(new DefaultHttpContext().Request, new AuthOptions()));
    }
}
