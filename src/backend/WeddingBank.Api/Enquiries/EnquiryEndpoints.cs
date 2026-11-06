using System.Net.Mail;
using WeddingBank.Application.Enquiries;

namespace WeddingBank.Api.Enquiries;

internal static class EnquiryEndpoints
{
    public static void MapEnquiryEndpoints(this WebApplication app)
    {
        app.MapPost("/api/enquiries", SendAsync).AllowAnonymous().RequireRateLimiting("enquiries");
    }

    private static async Task<IResult> SendAsync(
        EnquiryRequest request,
        IEnquirySender sender,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors is not null)
        {
            return Results.ValidationProblem(errors);
        }

        var enquiry = new Enquiry(
            OneLine(request.Name!),
            request.Email!.Trim(),
            string.IsNullOrWhiteSpace(request.Telephone) ? null : OneLine(request.Telephone),
            OneLine(request.Subject!),
            request.Message!.Trim());

        try
        {
            await sender.SendAsync(enquiry, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            loggerFactory.CreateLogger("WeddingBank.Enquiries").LogError(exception, "Enquiry mail was not accepted.");
            return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "The enquiry could not be sent.");
        }

        return Results.NoContent();
    }

    private static Dictionary<string, string[]>? Validate(EnquiryRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name) || OneLine(request.Name).Length is < 1 or > 120)
        {
            errors["name"] = ["Enter a name."];
        }

        var email = request.Email?.Trim() ?? "";
        if (email.Length is < 3 or > 200 || !IsMailbox(email))
        {
            errors["email"] = ["Enter an email address."];
        }

        if (!string.IsNullOrWhiteSpace(request.Telephone) && OneLine(request.Telephone).Length > 40)
        {
            errors["telephone"] = ["Enter a shorter telephone number."];
        }

        if (string.IsNullOrWhiteSpace(request.Subject) || OneLine(request.Subject).Length is < 1 or > 160)
        {
            errors["subject"] = ["Enter a subject."];
        }

        if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Trim().Length is < 1 or > 4000)
        {
            errors["message"] = ["Enter a message."];
        }

        return errors.Count == 0 ? null : errors;
    }

    private static bool IsMailbox(string email)
    {
        return !email.Contains(' ')
            && MailAddress.TryCreate(email, out var parsed)
            && parsed.Address == email;
    }

    private static string OneLine(string value)
    {
        return string.Join(' ', value.Split(['\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries)).Trim();
    }
}

internal sealed class EnquiryRequest
{
    public string? Name { get; init; }

    public string? Email { get; init; }

    public string? Telephone { get; init; }

    public string? Subject { get; init; }

    public string? Message { get; init; }
}
