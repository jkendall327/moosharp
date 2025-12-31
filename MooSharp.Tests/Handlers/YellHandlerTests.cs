using MooSharp.Actors.Rooms;
using MooSharp.Commands.Commands.Social;
using MooSharp.Commands.Presentation;

namespace MooSharp.Tests.Handlers;

public class YellHandlerTests
{
    [Fact]
    public async Task YellHandler_BroadcastsToRoomAndConnectedRooms()
    {
        var room1 = HandlerTestHelpers.CreateRoom("room1");
        var room2 = HandlerTestHelpers.CreateRoom("room2");
        var room3 = HandlerTestHelpers.CreateRoom("room3");

        // Setup connections
        room1.Exits.Add(new Exit
        {
            Name = "north",
            Description = "To room 2",
            Destination = room2.Id
        });

        // Room 3 is not connected to Room 1

        var world = await HandlerTestHelpers.CreateWorld([room1, room2, room3]);

        var yeller = HandlerTestHelpers.CreatePlayer("Yeller");
        var listenerInSameRoom = HandlerTestHelpers.CreatePlayer("ListenerInSameRoom");
        var listenerInConnectedRoom = HandlerTestHelpers.CreatePlayer("ListenerInConnectedRoom");
        var listenerInDisconnectedRoom = HandlerTestHelpers.CreatePlayer("ListenerInDisconnectedRoom");

        world.MovePlayer(yeller, room1);
        world.MovePlayer(listenerInSameRoom, room1);
        world.MovePlayer(listenerInConnectedRoom, room2);
        world.MovePlayer(listenerInDisconnectedRoom, room3);

        var handler = new YellHandler(world);

        var result = await handler.Handle(new()
        {
            Player = yeller,
            Message = "Hello World"
        });

        // Yeller gets feedback
        var actorMessage = Assert.Single(result.Messages, m => m.Player == yeller);
        Assert.Equal(MessageAudience.Actor, actorMessage.Audience);
        Assert.IsType<PlayerYelledEvent>(actorMessage.Event);
        Assert.Equal("Hello World", ((PlayerYelledEvent)actorMessage.Event).Message);

        // Listener in same room gets message
        var sameRoomMessage = Assert.Single(result.Messages, m => m.Player == listenerInSameRoom);
        Assert.Equal(MessageAudience.Observer, sameRoomMessage.Audience);
        Assert.IsType<PlayerYelledEvent>(sameRoomMessage.Event);
        Assert.Equal("Hello World", ((PlayerYelledEvent)sameRoomMessage.Event).Message);

        // Listener in connected room gets message
        var connectedRoomMessage = Assert.Single(result.Messages, m => m.Player == listenerInConnectedRoom);
        Assert.Equal(MessageAudience.Observer, connectedRoomMessage.Audience);
        Assert.IsType<PlayerHeardYellEvent>(connectedRoomMessage.Event);
        Assert.Equal("Hello World", ((PlayerHeardYellEvent)connectedRoomMessage.Event).Message);

        // Listener in disconnected room gets nothing
        Assert.DoesNotContain(result.Messages, m => m.Player == listenerInDisconnectedRoom);
    }
}
