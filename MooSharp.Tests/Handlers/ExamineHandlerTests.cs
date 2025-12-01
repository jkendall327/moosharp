namespace MooSharp.Tests;

public class ExamineHandlerTests
{
    [Fact]
    public async Task ExamineHandler_ReturnsRoomDescriptionWhenNoTarget()
    {
        var room = HandlerTestHelpers.CreateRoom("room");
        var world = await HandlerTestHelpers.CreateWorld(room);

        var player = HandlerTestHelpers.CreatePlayer();
        world.MovePlayer(player, room);

        var handler = new ExamineHandler(world, new());

        var result = await handler.Handle(new()
        {
            Player = player,
            Target = string.Empty
        });

        var message = Assert.Single(result.Messages);
        var evt = Assert.IsType<RoomDescriptionEvent>(message.Event);
        Assert.Equal(room.DescribeFor(player, useLongDescription: true), evt.Description);
    }

    [Fact]
    public async Task ExamineHandler_ReturnsSelfInventoryWhenTargetIsMe()
    {
        var room = HandlerTestHelpers.CreateRoom("room");
        var world = await HandlerTestHelpers.CreateWorld(room);

        var player = HandlerTestHelpers.CreatePlayer();
        world.MovePlayer(player, room);

        var item = new Object
        {
            Name = "Lantern",
            Description = "An old lantern"
        };

        item.MoveTo(player);

        var handler = new ExamineHandler(world, new());

        var result = await handler.Handle(new()
        {
            Player = player,
            Target = "me"
        });

        var message = Assert.Single(result.Messages);
        var evt = Assert.IsType<SelfExaminedEvent>(message.Event);
        Assert.Contains(item, evt.Inventory);
    }

    [Fact]
    public async Task ExamineHandler_ReturnsObjectDetailsWhenFound()
    {
        var room = HandlerTestHelpers.CreateRoom("room");
        var world = await HandlerTestHelpers.CreateWorld(room);

        var player = HandlerTestHelpers.CreatePlayer();
        world.MovePlayer(player, room);

        var item = new Object
        {
            Name = "Scroll",
            Description = "A dusty scroll"
        };

        item.MoveTo(room);

        var handler = new ExamineHandler(world, new());

        var result = await handler.Handle(new()
        {
            Player = player,
            Target = "Scroll"
        });

        var message = Assert.Single(result.Messages);
        var evt = Assert.IsType<ObjectExaminedEvent>(message.Event);
        Assert.Same(item, evt.Item);
    }
}