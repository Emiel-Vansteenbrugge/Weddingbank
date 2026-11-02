namespace WeddingBank.Application.Enquiries;

public sealed record Enquiry(
    string Name,
    string Email,
    string? Telephone,
    string Subject,
    string Message);
