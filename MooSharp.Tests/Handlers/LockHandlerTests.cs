namespace MooSharp.Tests;

public class LockHandlerTests
{
    [Fact]
    public async Task LockHandler_LocksItemWithMatchingKey()
    {
        var room = HandlerTestHelpers.CreateRoom("room");
        var world = await HandlerTestHelpers.CreateWorld(room);

        var player = HandlerTestHelpers.CreatePlayer();
        world.MovePlayer(player, room);

        var chest = new Object
        {
            Name = "Chest",
            Description = "A wooden chest",
            IsLockable = true,
            KeyId = "key-1"
        };

        var key = new Object
        {
            Name = "Key",
            Description = "A small key",
            KeyId = "key-1"
        };

        chest.MoveTo(room);
        key.MoveTo(player);

        var handler = new LockHandler(world, new());

        var result = await handler.Handle(new()
        {
            Player = player,
            Target = "chest"
        });

        var message = Assert.Single(result.Messages);
        Assert.IsType<ItemLockedEvent>(message.Event);
        Assert.True(chest.IsLocked);
    }

    [Fact]
    public async Task LockHandler_RequiresKey()
    {
        var room = HandlerTestHelpers.CreateRoom("room");
        var world = await HandlerTestHelpers.CreateWorld(room);

        var player = HandlerTestHelpers.CreatePlayer();
        world.MovePlayer(player, room);

        var chest = new Object
        {
            Name = "Chest",
            Description = "A wooden chest",
            IsLockable = true,
            KeyId = "key-1"
        };

        chest.MoveTo(room);

        var handler = new LockHandler(world, new());

        var result = await handler.Handle(new()
        {
            Player = player,
            Target = "chest"
        });

        var message = Assert.Single(result.Messages);
        Assert.IsType<SystemMessageEvent>(message.Event);
        Assert.False(chest.IsLocked);
    }

    [Fact]
    public async Task UnlockHandler_UnlocksWithMatchingKey()
    {
        var room = HandlerTestHelpers.CreateRoom("room");
        var world = await HandlerTestHelpers.CreateWorld(room);

        var player = HandlerTestHelpers.CreatePlayer();
        world.MovePlayer(player, room);

        var chest = new Object
        {
            Name = "Chest",
            Description = "A wooden chest",
            IsLockable = true,
            IsLocked = true,
            KeyId = "key-1"
        };

        var key = new Object
        {
            Name = "Key",
            Description = "A small key",
            KeyId = "key-1"
        };

        chest.MoveTo(room);
        key.MoveTo(player);

        var handler = new UnlockHandler(world, new());

        var result = await handler.Handle(new()
        {
            Player = player,
            Target = "chest"
        });

        var message = Assert.Single(result.Messages);
        Assert.IsType<ItemUnlockedEvent>(message.Event);
        Assert.False(chest.IsLocked);
    }

    [Fact]
    public async Task UnlockHandler_RequiresLock()
    {
        var room = HandlerTestHelpers.CreateRoom("room");
        var world = await HandlerTestHelpers.CreateWorld(room);

        var player = HandlerTestHelpers.CreatePlayer();
        world.MovePlayer(player, room);

        var chest = new Object
        {
            Name = "Chest",
            Description = "A wooden chest",
            IsLockable = true,
            KeyId = "key-1"
        };

        var key = new Object
        {
            Name = "Key",
            Description = "A small key",
            KeyId = "key-1"
        };

        chest.MoveTo(room);
        key.MoveTo(player);

        var handler = new UnlockHandler(world, new());

        var result = await handler.Handle(new()
        {
            Player = player,
            Target = "chest"
        });

        var message = Assert.Single(result.Messages);
        Assert.IsType<SystemMessageEvent>(message.Event);
        Assert.False(chest.IsLocked);
    }
}
