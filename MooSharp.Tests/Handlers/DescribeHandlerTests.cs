using MooSharp.Commands.Commands.Creative;
using MooSharp.Commands.Presentation;
using MooSharp.Tests.TestDoubles;

namespace MooSharp.Tests.Handlers;

public class DescribeHandlerTests
{

    [Fact]
    public async Task DescribeHandler_UpdatesCurrentRoomDescriptions()
    {
        var store = new InMemoryWorldRepository();
        var player = HandlerTestHelpers.CreatePlayer();
        var room = HandlerTestHelpers.CreateRoom("room", player.Username);
        var world = await HandlerTestHelpers.CreateWorld(store, room);
        world.MovePlayer(player, room);

        var handler = new DescribeHandler(world);

        var result = await handler.Handle(new()
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
        var store = new InMemoryWorldRepository();
        var player = HandlerTestHelpers.CreatePlayer();
        var origin = HandlerTestHelpers.CreateRoom("origin", player.Username);
        var destination = HandlerTestHelpers.CreateRoom("destination", player.Username);
        origin.Exits.Add("east", destination.Id);

        var world = await HandlerTestHelpers.CreateWorld(store, origin, destination);
        world.MovePlayer(player, origin);

        var handler = new DescribeHandler(world);

        var result = await handler.Handle(new()
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

    [Fact]
    public async Task DescribeHandler_PreventsNonCreatorFromUpdatingRoom()
    {
        var store = new InMemoryWorldRepository();
        var room = HandlerTestHelpers.CreateRoom("room", "Builder");
        var world = await HandlerTestHelpers.CreateWorld(store, room);
        var player = HandlerTestHelpers.CreatePlayer("NotBuilder");
        world.MovePlayer(player, room);

        var handler = new DescribeHandler(world);

        var result = await handler.Handle(new()
        {
            Player = player,
            Target = "here",
            Description = "New description"
        });

        var message = Assert.IsType<SystemMessageEvent>(Assert.Single(result.Messages).Event);
        Assert.Equal("You can only describe rooms you created.", message.Message);
        Assert.NotEqual("New description", room.Description);
    }
}