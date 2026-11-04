namespace WeddingBank.Domain.Clients;

public sealed class ClientUser
{
    private ClientUser()
    {
    }

    public Guid ClientId { get; private set; }

    public Guid UserId { get; private set; }

    public DateTimeOffset LinkedAt { get; private set; }

    public static ClientUser Link(Guid clientId, Guid userId, DateTimeOffset linkedAt)
    {
        if (clientId == Guid.Empty || userId == Guid.Empty)
        {
            throw new ArgumentException("A client association needs a client and a user.");
        }

        return new ClientUser
        {
            ClientId = clientId,
            UserId = userId,
            LinkedAt = linkedAt,
        };
    }
}
