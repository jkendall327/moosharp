namespace MooSharp.Tests;

public class ReadHandlerTests
{
    
    [Fact]
    public async Task ReadHandler_ReturnsObjectReadEventWhenTextPresent()
    {
        var room = HandlerTestHelpers.CreateRoom("room");
        var world = await HandlerTestHelpers.CreateWorld(room);

        var player = HandlerTestHelpers.CreatePlayer();
        world.MovePlayer(player, room);

        var item = new Object
        {
            Name = "Note",
            Description = "A folded note"
        };

        item.WriteText("Meet me later");

        item.MoveTo(room);

        var handler = new ReadHandler(world, new TargetResolver());

        var result = await handler.Handle(new ReadCommand
        {
            Player = player,
            Target = "Note"
        });

        var message = Assert.Single(result.Messages);
        var evt = Assert.IsType<ObjectReadEvent>(message.Event);
        Assert.Same(item, evt.Item);
    }

    [Fact]
    public async Task ReadHandler_ReturnsSystemMessageWhenNoText()
    {
        var room = HandlerTestHelpers.CreateRoom("room");
        var world = await HandlerTestHelpers.CreateWorld(room);

        var player = HandlerTestHelpers.CreatePlayer();
        world.MovePlayer(player, room);

        var item = new Object
        {
            Name = "Note",
            Description = "A folded note"
        };

        item.MoveTo(room);

        var handler = new ReadHandler(world, new TargetResolver());

        var result = await handler.Handle(new ReadCommand
        {
            Player = player,
            Target = "Note"
        });

        var message = Assert.Single(result.Messages);
        Assert.IsType<SystemMessageEvent>(message.Event);
    }
}