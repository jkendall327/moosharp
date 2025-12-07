using MooSharp.Commands.Commands.Creative;
using MooSharp.Commands.Presentation;
using MooSharp.Tests.TestDoubles;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Tests.Handlers;

public class RenameHandlerTests
{
    [Fact]
    public async Task RenameHandler_RenamesOwnedRoom()
    {
        var store = new InMemoryWorldRepository();
        var player = HandlerTestHelpers.CreatePlayer();
        var room = HandlerTestHelpers.CreateRoom("room", player.Username);
        var world = await HandlerTestHelpers.CreateWorld(store, room);
        world.MovePlayer(player, room);

        var handler = new RenameHandler(world, new());

        var result = await handler.Handle(new()
        {
            Player = player,
            Target = "here",
            NewName = "Cozy Nook"
        });

        var renameEvent = Assert.IsType<RoomRenamedEvent>(Assert.Single(result.Messages).Event);
        Assert.Equal("room name", renameEvent.OldName);
        Assert.Equal("Cozy Nook", renameEvent.NewName);
        Assert.Equal("Cozy Nook", room.Name);

        var persisted = (await store.LoadRoomsAsync()).Single();
        Assert.Equal("Cozy Nook", persisted.Name);
    }

    [Fact]
    public async Task RenameHandler_PreventsRenamingRoomNotOwned()
    {
        var store = new InMemoryWorldRepository();
        var player = HandlerTestHelpers.CreatePlayer();
        var room = HandlerTestHelpers.CreateRoom("room", "Other");
        var world = await HandlerTestHelpers.CreateWorld(store, room);
        world.MovePlayer(player, room);

        var handler = new RenameHandler(world, new());

        var result = await handler.Handle(new()
        {
            Player = player,
            Target = "here",
            NewName = "Denied"
        });

        var message = Assert.IsType<SystemMessageEvent>(Assert.Single(result.Messages).Event);
        Assert.Equal("You can only rename rooms you created.", message.Message);
        Assert.Equal("room name", room.Name);
    }

    [Fact]
    public async Task RenameHandler_RenamesOwnedItem()
    {
        var store = new InMemoryWorldRepository();
        var player = HandlerTestHelpers.CreatePlayer();
        var room = HandlerTestHelpers.CreateRoom("room", player.Username);
        var item = new Object
        {
            Name = "lantern",
            Description = "A lantern",
            CreatorUsername = player.Username
        };
        item.MoveTo(room);

        var world = await HandlerTestHelpers.CreateWorld(store, room);
        world.MovePlayer(player, room);

        var handler = new RenameHandler(world, new());

        var result = await handler.Handle(new()
        {
            Player = player,
            Target = "lantern",
            NewName = "torch"
        });

        var renameEvent = Assert.IsType<ObjectRenamedEvent>(Assert.Single(result.Messages).Event);
        Assert.Equal("lantern", renameEvent.OldName);
        Assert.Equal("torch", renameEvent.NewName);
        Assert.Equal("torch", item.Name);
    }

    [Fact]
    public async Task RenameHandler_PreventsRenamingUnownedItem()
    {
        var store = new InMemoryWorldRepository();
        var player = HandlerTestHelpers.CreatePlayer();
        var room = HandlerTestHelpers.CreateRoom("room", player.Username);
        var item = new Object
        {
            Name = "book",
            Description = "A dusty book",
            CreatorUsername = "Other"
        };
        item.MoveTo(room);

        var world = await HandlerTestHelpers.CreateWorld(store, room);
        world.MovePlayer(player, room);

        var handler = new RenameHandler(world, new());

        var result = await handler.Handle(new()
        {
            Player = player,
            Target = "book",
            NewName = "journal"
        });

        var message = Assert.IsType<SystemMessageEvent>(Assert.Single(result.Messages).Event);
        Assert.Equal("You can only rename items you created.", message.Message);
        Assert.Equal("book", item.Name);
    }
}
