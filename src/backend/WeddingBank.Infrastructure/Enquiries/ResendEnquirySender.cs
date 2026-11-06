using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using WeddingBank.Application.Enquiries;

namespace WeddingBank.Infrastructure.Enquiries;

public sealed class ResendEnquirySender(HttpClient http, IOptions<ResendOptions> options) : IEnquirySender
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task SendAsync(Enquiry enquiry, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey) ||
            string.IsNullOrWhiteSpace(settings.FromEmail) ||
            string.IsNullOrWhiteSpace(settings.To))
        {
            throw new InvalidOperationException("Enquiry mail is not configured.");
        }

        var telephone = string.IsNullOrWhiteSpace(enquiry.Telephone) ? "Not provided" : enquiry.Telephone;
        var text = $"""
            Name: {enquiry.Name}
            Email: {enquiry.Email}
            Telephone: {telephone}
            Subject: {enquiry.Subject}

            {enquiry.Message}
            """;
        var payload = new Dictionary<string, object?>
        {
            ["from"] = settings.FromEmail,
            ["to"] = new[] { settings.To },
            ["reply_to"] = enquiry.Email,
            ["subject"] = "Website enquiry: " + enquiry.Subject,
            ["text"] = text,
            ["html"] = $"""
                <p><strong>Name:</strong> {Encode(enquiry.Name)}<br>
                <strong>Email:</strong> {Encode(enquiry.Email)}<br>
                <strong>Telephone:</strong> {Encode(telephone)}<br>
                <strong>Subject:</strong> {Encode(enquiry.Subject)}</p>
                <p>{Encode(enquiry.Message).Replace("\n", "<br>", StringComparison.Ordinal)}</p>
                """,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey.Trim());
        request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var detail = body.Length > 500 ? body[..500] : body;
            throw new InvalidOperationException($"Resend returned {(int)response.StatusCode}. {detail}");
        }
    }

    private static string Encode(string value) => System.Net.WebUtility.HtmlEncode(value);
}
