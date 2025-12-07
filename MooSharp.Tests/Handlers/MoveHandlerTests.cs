using Microsoft.Extensions.Logging.Abstractions;
using MooSharp.Actors.Rooms;
using MooSharp.Commands.Commands;
using MooSharp.Commands.Presentation;

namespace MooSharp.Tests.Handlers;

public class MoveHandlerTests
{
    [Fact]
    public async Task MoveHandler_MovesPlayerAndAddsMovementEvents()
    {
        var origin = HandlerTestHelpers.CreateRoom("origin");
        var destination = HandlerTestHelpers.CreateRoom("destination");
        var exit = new Exit
        {
            Name = "north",
            Description = string.Empty,
            Destination = destination.Id
        };

        origin.Exits.Add(exit);

        var world = await HandlerTestHelpers.CreateWorld(origin, destination);

        var player = HandlerTestHelpers.CreatePlayer("Alice");
        world.MovePlayer(player, origin);

        var handler = new MoveHandler(world, NullLogger<MoveHandler>.Instance);

        var result = await handler.Handle(new()
        {
            Player = player,
            TargetExit = exit
        });

        Assert.Same(destination, world.GetPlayerLocation(player));

        var departed = Assert.Single(result.Messages, m => m.Event is PlayerDepartedEvent);
        Assert.Equal(MessageAudience.Actor, departed.Audience);
        Assert.Equal(player, departed.Player);
        Assert.Equal(origin, ((PlayerDepartedEvent)departed.Event).Origin);

        var moved = Assert.Single(result.Messages, m => m.Event is PlayerMovedEvent);
        Assert.Equal(destination, ((PlayerMovedEvent)moved.Event).Destination);

        var description = Assert.Single(result.Messages, m => m.Event is RoomDescriptionEvent);
        Assert.NotNull(((RoomDescriptionEvent)description.Event).Description);
    }

    [Fact]
    public async Task MoveHandler_BroadcastsToObservers()
    {
        var origin = HandlerTestHelpers.CreateRoom("origin");
        var destination = HandlerTestHelpers.CreateRoom("destination");
        var exit = new Exit
        {
            Name = "north",
            Description = string.Empty,
            Destination = destination.Id
        };

        origin.Exits.Add(exit);

        var world = await HandlerTestHelpers.CreateWorld(origin, destination);

        var actor = HandlerTestHelpers.CreatePlayer("Actor");
        var originObserver = HandlerTestHelpers.CreatePlayer("OriginObserver");
        var destinationObserver = HandlerTestHelpers.CreatePlayer("DestinationObserver");

        world.MovePlayer(actor, origin);
        world.MovePlayer(originObserver, origin);
        world.MovePlayer(destinationObserver, destination);

        var handler = new MoveHandler(world, NullLogger<MoveHandler>.Instance);

        var result = await handler.Handle(new()
        {
            Player = actor,
            TargetExit = exit
        });

        var originMessage = Assert.Single(result.Messages, m => m.Player == originObserver);
        Assert.Equal(MessageAudience.Observer, originMessage.Audience);
        Assert.IsType<PlayerDepartedEvent>(originMessage.Event);

        var destinationMessage = Assert.Single(result.Messages, m => m.Player == destinationObserver);
        Assert.Equal(MessageAudience.Observer, destinationMessage.Audience);
        Assert.IsType<PlayerArrivedEvent>(destinationMessage.Event);
    }
}