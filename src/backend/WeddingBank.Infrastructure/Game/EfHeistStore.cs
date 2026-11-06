using Microsoft.EntityFrameworkCore;
using WeddingBank.Application.Game;
using WeddingBank.Domain.Game;
using WeddingBank.Infrastructure.Persistence;

namespace WeddingBank.Infrastructure.Game;

public sealed class EfHeistStore(WeddingBankDbContext db) : IHeistStore
{
    public async Task<HeistRoom> GetOrBeginAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var room = await db.HeistRooms
            .SingleOrDefaultAsync(item => item.Id == HeistRoom.SingletonId, cancellationToken);
        if (room is not null)
        {
            return room;
        }

        room = HeistRoom.Begin(now);
        db.HeistRooms.Add(room);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return room;
        }
        catch (DbUpdateException)
        {
            db.Entry(room).State = EntityState.Detached;
            return await db.HeistRooms.SingleAsync(item => item.Id == HeistRoom.SingletonId, cancellationToken);
        }
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return db.SaveChangesAsync(cancellationToken);
    }
}
