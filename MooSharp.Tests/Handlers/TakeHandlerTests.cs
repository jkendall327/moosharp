using MooSharp.Commands.Commands.Informational;
using MooSharp.Commands.Commands.Items;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Tests.Handlers;

public class TakeHandlerTests
{
    [Fact]
    public async Task TakeHandler_MovesUnownedItemToPlayer()
    {
        var room = HandlerTestHelpers.CreateRoom("room");
        var world = await HandlerTestHelpers.CreateWorld(room);

        var player = HandlerTestHelpers.CreatePlayer();
        world.MovePlayer(player, room);

        var item = new Object
        {
            Name = "Lamp",
            Description = "A shiny lamp"
        };

        item.MoveTo(room);

        var handler = new TakeHandler(world, new());

        var result = await handler.Handle(new()
        {
            Player = player,
            Target = "Lamp"
        });

        var message = Assert.Single(result.Messages);
        Assert.IsType<ItemTakenEvent>(message.Event);
        Assert.Same(player, item.Owner);
        Assert.Contains(item, player.Inventory);
        Assert.DoesNotContain(item, room.Contents);
    }

    [Fact]
    public async Task TakeHandler_ReturnsNotFoundEventWhenMissing()
    {
        var room = HandlerTestHelpers.CreateRoom("room");
        var world = await HandlerTestHelpers.CreateWorld(room);

        var player = HandlerTestHelpers.CreatePlayer();
        world.MovePlayer(player, room);

        var handler = new TakeHandler(world, new());

        var result = await handler.Handle(new()
        {
            Player = player,
            Target = "MissingItem"
        });

        var message = Assert.Single(result.Messages);
        Assert.IsType<ItemNotFoundEvent>(message.Event);
        Assert.Empty(player.Inventory);
    }

    [Fact]
    public async Task TakeHandler_ReturnsOwnershipConflictWhenItemOwned()
    {
        var room = HandlerTestHelpers.CreateRoom("room");
        var world = await HandlerTestHelpers.CreateWorld(room);

        var owner = HandlerTestHelpers.CreatePlayer("Owner");
        var seeker = HandlerTestHelpers.CreatePlayer("Seeker");
        world.MovePlayer(owner, room);
        world.MovePlayer(seeker, room);

        var item = new Object
        {
            Name = "Gem",
            Description = "A bright gem"
        };

        item.MoveTo(owner);

        var handler = new TakeHandler(world, new());

        var result = await handler.Handle(new()
        {
            Player = seeker,
            Target = "Gem"
        });

        var message = Assert.Single(result.Messages);
        Assert.IsType<ItemOwnedByOtherEvent>(message.Event);
        Assert.Same(owner, item.Owner);
        Assert.DoesNotContain(item, seeker.Inventory);
    }

    [Fact]
    public async Task TakeHandler_ReturnsAlreadyOwnedWhenItemInInventory()
    {
        var room = HandlerTestHelpers.CreateRoom("room");
        var world = await HandlerTestHelpers.CreateWorld(room);

        var player = HandlerTestHelpers.CreatePlayer();
        world.MovePlayer(player, room);

        var item = new Object
        {
            Name = "Gem",
            Description = "A bright gem"
        };

        item.MoveTo(player);

        var handler = new TakeHandler(world, new());

        var result = await handler.Handle(new()
        {
            Player = player,
            Target = "Gem"
        });

        var message = Assert.Single(result.Messages);
        Assert.IsType<ItemAlreadyInPossessionEvent>(message.Event);
    }
}