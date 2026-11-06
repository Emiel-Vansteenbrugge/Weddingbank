using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WeddingBank.Tests;

public sealed class HealthEndpointTests : IClassFixture<HealthApiFixture>
{
    private readonly HealthApiFixture _fixture;

    public HealthEndpointTests(HealthApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Health_returns_success()
    {
        using var client = _fixture.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

public sealed class HealthApiFixture : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseSetting("Documents:DeferReadBaseline", "true");
    }
}
