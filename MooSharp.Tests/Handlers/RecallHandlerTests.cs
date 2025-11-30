using MooSharp.Messaging;

namespace MooSharp.Tests;

public class RecallHandlerTests
{
    [Fact]
    public async Task RecallHandler_MovesPlayerToDefaultRoom()
    {
        var atrium = HandlerTestHelpers.CreateRoom("atrium");
        var branch = HandlerTestHelpers.CreateRoom("branch");

        var world = await HandlerTestHelpers.CreateWorld(atrium, branch);

        var player = HandlerTestHelpers.CreatePlayer("Explorer");
        world.MovePlayer(player, branch);

        var handler = new RecallHandler(world);

        var result = await handler.Handle(new RecallCommand
        {
            Player = player
        });

        Assert.Same(atrium, world.GetPlayerLocation(player));

        Assert.Contains(result.Messages, m => m.Player == player && m.Event is PlayerRecalledEvent);
        Assert.Contains(result.Messages, m => m.Player == player && m.Event is PlayerMovedEvent);
        Assert.Contains(result.Messages, m => m.Player == player && m.Event is RoomDescriptionEvent);
    }

    [Fact]
    public async Task RecallHandler_NotifiesObserversAtOriginAndDestination()
    {
        var atrium = HandlerTestHelpers.CreateRoom("atrium");
        var branch = HandlerTestHelpers.CreateRoom("branch");

        var world = await HandlerTestHelpers.CreateWorld(atrium, branch);

        var actor = HandlerTestHelpers.CreatePlayer("Actor");
        var originObserver = HandlerTestHelpers.CreatePlayer("OriginObserver");
        var destinationObserver = HandlerTestHelpers.CreatePlayer("DestinationObserver");

        world.MovePlayer(actor, branch);
        world.MovePlayer(originObserver, branch);
        world.MovePlayer(destinationObserver, atrium);

        var handler = new RecallHandler(world);

        var result = await handler.Handle(new RecallCommand
        {
            Player = actor
        });

        Assert.Single(result.Messages, m =>
            m.Player == originObserver && m.Audience == MessageAudience.Observer && m.Event is PlayerRecalledEvent);

        Assert.Single(result.Messages, m =>
            m.Player == destinationObserver && m.Audience == MessageAudience.Observer && m.Event is PlayerArrivedEvent);
    }
}
