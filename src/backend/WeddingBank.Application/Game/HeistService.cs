using WeddingBank.Domain.Game;

namespace WeddingBank.Application.Game;

public sealed record SafeView(bool Present, int AmountEuros, string Currency, bool CombinationRevealed);

public sealed record HeistView(
    string Title,
    string Status,
    string Stage,
    DateTimeOffset UpdatedAt,
    SafeView Safe);

public interface IHeistStore
{
    Task<HeistRoom> GetOrBeginAsync(DateTimeOffset now, CancellationToken cancellationToken);

    Task SaveAsync(CancellationToken cancellationToken);
}

public sealed class HeistService(IHeistStore store, TimeProvider clock)
{
    public const string Title = "Wedding Bank – The Great Heist";

    public async Task<HeistView> ReadAsync(CancellationToken cancellationToken)
    {
        var room = await store.GetOrBeginAsync(clock.GetUtcNow(), cancellationToken);
        await store.SaveAsync(cancellationToken);
        return View(room);
    }

    public Task<HeistView> OpenAsync(CancellationToken cancellationToken)
    {
        return ChangeAsync(room => room.Open(clock.GetUtcNow()), cancellationToken);
    }

    public Task<HeistView> CloseAsync(CancellationToken cancellationToken)
    {
        return ChangeAsync(room => room.Close(clock.GetUtcNow()), cancellationToken);
    }

    private async Task<HeistView> ChangeAsync(Action<HeistRoom> change, CancellationToken cancellationToken)
    {
        var room = await store.GetOrBeginAsync(clock.GetUtcNow(), cancellationToken);
        change(room);
        await store.SaveAsync(cancellationToken);
        return View(room);
    }

    private static HeistView View(HeistRoom room)
    {
        return new HeistView(
            Title,
            room.Status,
            room.Stage,
            room.UpdatedAt,
            new SafeView(PhysicalSafe.Present, PhysicalSafe.AmountEuros, PhysicalSafe.Currency, CombinationRevealed: false));
    }
}
