using MooSharp.Messaging;

namespace MooSharp.Tests;

public class WriteHandlerTests
{
    [Fact]
    public async Task WriteHandler_WritesOnRoomObjectAndBroadcasts()
    {
        var room = HandlerTestHelpers.CreateRoom("room");
        var world = await HandlerTestHelpers.CreateWorld(room);

        var writer = HandlerTestHelpers.CreatePlayer("Writer");
        var observer = HandlerTestHelpers.CreatePlayer("Observer");
        world.MovePlayer(writer, room);
        world.MovePlayer(observer, room);

        var item = new Object
        {
            Name = "Sign",
            Description = "A wooden sign"
        };

        item.MoveTo(room);

        var handler = new WriteHandler(world, new TargetResolver());

        var result = await handler.Handle(new WriteCommand
        {
            Player = writer,
            Target = "Sign",
            Text = "welcome"
        });

        Assert.Equal("welcome", item.TextContent);

        var actorMessage = Assert.Single(result.Messages, m => m.Player == writer);
        Assert.IsType<ObjectWrittenOnEvent>(actorMessage.Event);

        var observerMessage = Assert.Single(result.Messages, m => m.Player == observer);
        Assert.Equal(MessageAudience.Observer, observerMessage.Audience);
        Assert.IsType<ObjectWrittenOnEvent>(observerMessage.Event);
    }
}