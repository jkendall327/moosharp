using Microsoft.Extensions.Logging.Abstractions;
using MooSharp.Messaging;

namespace MooSharp.Tests;

public class MoveHandlerTests
{
    [Fact]
    public async Task MoveHandler_MovesPlayerAndAddsMovementEvents()
    {
        var origin = HandlerTestHelpers.CreateRoom("origin");
        var destination = HandlerTestHelpers.CreateRoom("destination");
        origin.Exits.Add("north", destination.Id);

        var world = await HandlerTestHelpers.CreateWorld(origin, destination);

        var player = HandlerTestHelpers.CreatePlayer("Alice");
        world.MovePlayer(player, origin);

        var handler = new MoveHandler(world, NullLogger<MoveHandler>.Instance);

        var result = await handler.Handle(new()
        {
            Player = player,
            TargetExit = "north"
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
    public async Task MoveHandler_ReturnsExitNotFoundWhenMissing()
    {
        var origin = HandlerTestHelpers.CreateRoom("origin");
        var world = await HandlerTestHelpers.CreateWorld(origin);

        var player = HandlerTestHelpers.CreatePlayer();
        world.MovePlayer(player, origin);

        var handler = new MoveHandler(world, NullLogger<MoveHandler>.Instance);

        var result = await handler.Handle(new()
        {
            Player = player,
            TargetExit = "south"
        });

        var failure = Assert.Single(result.Messages);
        Assert.IsType<ExitNotFoundEvent>(failure.Event);
        Assert.Same(origin, world.GetPlayerLocation(player));
    }

    [Fact]
    public async Task MoveHandler_BroadcastsToObservers()
    {
        var origin = HandlerTestHelpers.CreateRoom("origin");
        var destination = HandlerTestHelpers.CreateRoom("destination");
        origin.Exits.Add("north", destination.Id);

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
            TargetExit = "north"
        });

        var originMessage = Assert.Single(result.Messages, m => m.Player == originObserver);
        Assert.Equal(MessageAudience.Observer, originMessage.Audience);
        Assert.IsType<PlayerDepartedEvent>(originMessage.Event);

        var destinationMessage = Assert.Single(result.Messages, m => m.Player == destinationObserver);
        Assert.Equal(MessageAudience.Observer, destinationMessage.Audience);
        Assert.IsType<PlayerArrivedEvent>(destinationMessage.Event);
    }
}