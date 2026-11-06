namespace WeddingBank.Domain.Game;

public static class HeistStatuses
{
    public const string Closed = "CLOSED";

    public const string Open = "OPEN";
}

/// <summary>
/// The physical safe is part of the game. Its combination is not stored here.
/// </summary>
public static class PhysicalSafe
{
    public const bool Present = true;

    public const int AmountEuros = 250;

    public const string Currency = "EUR";
}

public sealed class HeistRoom
{
    public const int SingletonId = 1;

    public const string Entrance = "entrance";

    private HeistRoom()
    {
    }

    public int Id { get; private set; }

    public string Status { get; private set; } = HeistStatuses.Closed;

    public string Stage { get; private set; } = Entrance;

    public DateTimeOffset UpdatedAt { get; private set; }

    public static HeistRoom Begin(DateTimeOffset now)
    {
        return new HeistRoom
        {
            Id = SingletonId,
            Status = HeistStatuses.Closed,
            Stage = Entrance,
            UpdatedAt = now,
        };
    }

    public void Open(DateTimeOffset now)
    {
        Status = HeistStatuses.Open;
        Stage = Entrance;
        UpdatedAt = now;
    }

    public void Close(DateTimeOffset now)
    {
        Status = HeistStatuses.Closed;
        Stage = Entrance;
        UpdatedAt = now;
    }
}
