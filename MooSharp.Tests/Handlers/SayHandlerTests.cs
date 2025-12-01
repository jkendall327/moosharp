using MooSharp.Commands;
using MooSharp.Commands.Commands.Social;
using MooSharp.Messaging;

namespace MooSharp.Tests.Handlers;

public class SayHandlerTests
{
    [Fact]
    public async Task SayHandler_BroadcastsToRoom()
    {
        var room = HandlerTestHelpers.CreateRoom("room");
        var world = await HandlerTestHelpers.CreateWorld(room);

        var speaker = HandlerTestHelpers.CreatePlayer("Speaker");
        var listener = HandlerTestHelpers.CreatePlayer("Listener");
        world.MovePlayer(speaker, room);
        world.MovePlayer(listener, room);

        var handler = new SayHandler(world);

        var result = await handler.Handle(new()
        {
            Player = speaker,
            Message = " Hello there "
        });

        var actorMessage = Assert.Single(result.Messages, m => m.Player == speaker);
        Assert.Equal(MessageAudience.Actor, actorMessage.Audience);
        Assert.IsType<PlayerSaidEvent>(actorMessage.Event);

        var observerMessage = Assert.Single(result.Messages, m => m.Player == listener);
        Assert.Equal(MessageAudience.Observer, observerMessage.Audience);
        Assert.IsType<PlayerSaidEvent>(observerMessage.Event);
    }

    [Fact]
    public async Task SayHandler_ReturnsSystemMessageForEmptyContent()
    {
        var room = HandlerTestHelpers.CreateRoom("room");
        var world = await HandlerTestHelpers.CreateWorld(room);

        var speaker = HandlerTestHelpers.CreatePlayer();
        world.MovePlayer(speaker, room);

        var handler = new SayHandler(world);

        var result = await handler.Handle(new()
        {
            Player = speaker,
            Message = "   "
        });

        var message = Assert.Single(result.Messages);
        var evt = Assert.IsType<SystemMessageEvent>(message.Event);
        Assert.False(string.IsNullOrWhiteSpace(evt.Message));
    }
}