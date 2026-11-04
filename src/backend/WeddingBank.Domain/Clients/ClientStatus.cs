namespace WeddingBank.Domain.Clients;

public enum ClientStatus
{
    Active,
    Inactive,
    Suspended,
}

public static class ClientStatuses
{
    public const string Active = "ACTIVE";
    public const string Inactive = "INACTIVE";
    public const string Suspended = "SUSPENDED";

    public static string ToName(ClientStatus status)
    {
        return status switch
        {
            ClientStatus.Active => Active,
            ClientStatus.Inactive => Inactive,
            ClientStatus.Suspended => Suspended,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown client status."),
        };
    }

    public static bool TryParse(string? value, out ClientStatus status)
    {
        switch (value?.Trim().ToUpperInvariant())
        {
            case Active:
                status = ClientStatus.Active;
                return true;
            case Inactive:
                status = ClientStatus.Inactive;
                return true;
            case Suspended:
                status = ClientStatus.Suspended;
                return true;
            default:
                status = default;
                return false;
        }
    }

    public static bool CanMove(ClientStatus from, ClientStatus to)
    {
        return (from, to) switch
        {
            (ClientStatus.Active, ClientStatus.Inactive) => true,
            (ClientStatus.Active, ClientStatus.Suspended) => true,
            (ClientStatus.Inactive, ClientStatus.Active) => true,
            (ClientStatus.Suspended, ClientStatus.Active) => true,
            (ClientStatus.Suspended, ClientStatus.Inactive) => true,
            _ => false,
        };
    }
}
