using MooSharp.Tests.TestDoubles;

namespace MooSharp.Tests;

public class DescribeHandlerTests
{

    [Fact]
    public async Task DescribeHandler_UpdatesCurrentRoomDescriptions()
    {
        var store = new InMemoryWorldStore();
        var room = HandlerTestHelpers.CreateRoom("room");
        var world = await HandlerTestHelpers.CreateWorld(store, room);

        var player = HandlerTestHelpers.CreatePlayer();
        world.MovePlayer(player, room);

        var handler = new DescribeHandler(world);

        var result = await handler.Handle(new DescribeCommand
        {
            Player = player,
            Target = "here",
            Description = "A cozy den"
        });

        var updateEvent = Assert.Single(result.Messages)
            .Event as RoomDescriptionUpdatedEvent;

        Assert.NotNull(updateEvent);
        Assert.Equal("A cozy den", room.Description);
        Assert.Equal("A cozy den", room.LongDescription);

        var persisted = (await store.LoadRoomsAsync()).Single();
        Assert.Equal("A cozy den", persisted.Description);
        Assert.Equal("A cozy den", persisted.LongDescription);
    }

    [Fact]
    public async Task DescribeHandler_UpdatesExitRoomDescription()
    {
        var store = new InMemoryWorldStore();
        var origin = HandlerTestHelpers.CreateRoom("origin");
        var destination = HandlerTestHelpers.CreateRoom("destination");
        origin.Exits.Add("east", destination.Id);

        var world = await HandlerTestHelpers.CreateWorld(store, origin, destination);

        var player = HandlerTestHelpers.CreatePlayer();
        world.MovePlayer(player, origin);

        var handler = new DescribeHandler(world);

        var result = await handler.Handle(new DescribeCommand
        {
            Player = player,
            Target = "east",
            Description = "An airy annex"
        });

        Assert.Single(result.Messages, m => m.Event is RoomDescriptionUpdatedEvent);
        Assert.Equal("An airy annex", destination.Description);
        Assert.Equal("An airy annex", destination.LongDescription);

        var persisted = (await store.LoadRoomsAsync()).Single(r => r.Id == destination.Id);
        Assert.Equal("An airy annex", persisted.Description);
        Assert.Equal("An airy annex", persisted.LongDescription);
    }
}