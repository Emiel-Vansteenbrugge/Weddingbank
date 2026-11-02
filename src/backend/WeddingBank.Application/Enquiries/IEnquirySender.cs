namespace WeddingBank.Application.Enquiries;

public interface IEnquirySender
{
    Task SendAsync(Enquiry enquiry, CancellationToken cancellationToken);
}
