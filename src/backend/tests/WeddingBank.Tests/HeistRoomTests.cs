using WeddingBank.Domain.Game;

namespace WeddingBank.Tests;

public sealed class HeistRoomTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 6, 22, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Entering_keeps_both_players_inside()
    {
        var room = HeistRoom.Begin(Now);

        room.Enter(Now.AddMinutes(1));
        room.Enter(Now.AddMinutes(2));
        room.Open(Now.AddMinutes(3));
        room.Close(Now.AddMinutes(4));

        Assert.Equal(HeistRoom.Inside, room.Stage);
        Assert.Equal(HeistStatuses.Closed, room.Status);
    }

    [Fact]
    public void An_administrator_can_reset_the_room_to_the_entrance()
    {
        var room = HeistRoom.Begin(Now);
        room.Enter(Now.AddMinutes(1));

        room.ResetToEntrance(Now.AddMinutes(2));

        Assert.Equal(HeistRoom.Entrance, room.Stage);
    }

    [Fact]
    public void A_new_room_is_still_at_the_entrance()
    {
        var room = HeistRoom.Begin(Now);

        Assert.Equal(HeistRoom.Entrance, room.Stage);
    }
}
