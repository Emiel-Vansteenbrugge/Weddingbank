using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using WeddingBank.Application.Enquiries;
using WeddingBank.Infrastructure.Enquiries;

namespace WeddingBank.Tests;

public sealed class EnquiryEndpointTests
{
    [Fact]
    public async Task Post_sends_every_field()
    {
        var sender = new RecordingEnquirySender();
        await using var factory = CreateFactory(sender);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/enquiries", new
        {
            name = "Clara Meier",
            email = "clara.meier@example.com",
            telephone = "+41 79 000 00 00",
            subject = "Introduction",
            message = "I would like to arrange a conversation in Geneva.",
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var enquiry = Assert.Single(sender.Sent);
        Assert.Equal("Clara Meier", enquiry.Name);
        Assert.Equal("clara.meier@example.com", enquiry.Email);
        Assert.Equal("+41 79 000 00 00", enquiry.Telephone);
        Assert.Equal("Introduction", enquiry.Subject);
        Assert.Equal("I would like to arrange a conversation in Geneva.", enquiry.Message);
    }

    [Fact]
    public async Task Post_rejects_an_incomplete_enquiry()
    {
        var sender = new RecordingEnquirySender();
        await using var factory = CreateFactory(sender);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/enquiries", new
        {
            email = "not-an-email",
            subject = "Introduction",
            message = "Hello",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(sender.Sent);
    }

    [Fact]
    public async Task Post_reports_a_mail_failure_without_the_mailbox()
    {
        var sender = new RecordingEnquirySender { Failure = new InvalidOperationException("Resend returned 403.") };
        await using var factory = CreateFactory(sender);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/enquiries", new
        {
            name = "Clara Meier",
            email = "clara.meier@example.com",
            subject = "Introduction",
            message = "Hello",
        });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("hotmail", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Resend returned", body, StringComparison.Ordinal);
    }

    private static WebApplicationFactory<Program> CreateFactory(RecordingEnquirySender sender)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEnquirySender>();
                services.AddSingleton<IEnquirySender>(sender);
            });
        });
    }

    private sealed class RecordingEnquirySender : IEnquirySender
    {
        public List<Enquiry> Sent { get; } = [];

        public Exception? Failure { get; init; }

        public Task SendAsync(Enquiry enquiry, CancellationToken cancellationToken)
        {
            if (Failure is not null)
            {
                throw Failure;
            }

            Sent.Add(enquiry);
            return Task.CompletedTask;
        }
    }
}

public sealed class ResendEnquirySenderTests
{
    [Fact]
    public async Task Send_posts_the_enquiry_to_the_configured_mailbox()
    {
        var handler = new CaptureHandler(HttpStatusCode.OK);
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.resend.com/") };
        var sender = new ResendEnquirySender(http, Options.Create(new ResendOptions
        {
            ApiKey = "re_test",
            FromEmail = "\"Ambre & Jules\" <noreply.ambre@evsb.io>",
            To = "emielvansteenbrugge@hotmail.com",
        }));

        await sender.SendAsync(
            new Enquiry("Clara Meier", "clara.meier@example.com", null, "Introduction", "A conversation."),
            CancellationToken.None);

        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("https://api.resend.com/emails", handler.Request.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.Request.Headers.Authorization!.Scheme);
        Assert.Equal("re_test", handler.Request.Headers.Authorization.Parameter);

        using var document = JsonDocument.Parse(handler.Body!);
        var root = document.RootElement;
        Assert.Equal("\"Ambre & Jules\" <noreply.ambre@evsb.io>", root.GetProperty("from").GetString());
        Assert.Equal("emielvansteenbrugge@hotmail.com", root.GetProperty("to")[0].GetString());
        Assert.Equal("clara.meier@example.com", root.GetProperty("reply_to").GetString());
        Assert.Equal("Website enquiry: Introduction", root.GetProperty("subject").GetString());
        Assert.Contains("A conversation.", root.GetProperty("text").GetString(), StringComparison.Ordinal);
        Assert.Contains("Clara Meier", root.GetProperty("html").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Send_reports_a_rejected_response()
    {
        var handler = new CaptureHandler(HttpStatusCode.Forbidden);
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.resend.com/") };
        var sender = new ResendEnquirySender(http, Options.Create(new ResendOptions
        {
            ApiKey = "re_test",
            FromEmail = "Ambre & Jules <noreply.ambre@evsb.io>",
            To = "emielvansteenbrugge@hotmail.com",
        }));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendAsync(
                new Enquiry("Clara Meier", "clara.meier@example.com", null, "Introduction", "Hello"),
                CancellationToken.None));

        Assert.Contains("403", exception.Message, StringComparison.Ordinal);
    }

    private sealed class CaptureHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status)
            {
                Content = new StringContent("{\"message\":\"no\"}"),
            };
        }
    }
}
