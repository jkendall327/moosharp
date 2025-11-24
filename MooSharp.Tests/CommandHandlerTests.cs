using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MooSharp;
using MooSharp.Messaging;

namespace MooSharp.Tests;

public class CommandHandlerTests
{
    [Fact]
    public async Task MoveHandler_MovesPlayerAndAddsMovementEvents()
    {
        var world = CreateWorld();
        var origin = CreateRoom("origin");
        var destination = CreateRoom("destination");
        origin.Exits.Add("north", destination.Id);

        world.Rooms.Add(origin.Id, origin);
        world.Rooms.Add(destination.Id, destination);

        var player = CreatePlayer("Alice");
        origin.PlayersInRoom.Add(player);

        var handler = new MoveHandler(world, NullLogger<MoveHandler>.Instance);

        var result = await handler.Handle(new MoveCommand
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
        var world = CreateWorld();
        var origin = CreateRoom("origin");
        world.Rooms.Add(origin.Id, origin);

        var player = CreatePlayer();
        origin.PlayersInRoom.Add(player);

        var handler = new MoveHandler(world, NullLogger<MoveHandler>.Instance);

        var result = await handler.Handle(new MoveCommand
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
        var world = CreateWorld();
        var origin = CreateRoom("origin");
        var destination = CreateRoom("destination");
        origin.Exits.Add("north", destination.Id);

        world.Rooms.Add(origin.Id, origin);
        world.Rooms.Add(destination.Id, destination);

        var actor = CreatePlayer("Actor");
        var originObserver = CreatePlayer("OriginObserver");
        var destinationObserver = CreatePlayer("DestinationObserver");

        origin.PlayersInRoom.Add(actor);
        origin.PlayersInRoom.Add(originObserver);
        destination.PlayersInRoom.Add(destinationObserver);

        var handler = new MoveHandler(world, NullLogger<MoveHandler>.Instance);

        var result = await handler.Handle(new MoveCommand
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

    [Fact]
    public async Task TakeHandler_MovesUnownedItemToPlayer()
    {
        var world = CreateWorld();
        var room = CreateRoom("room");
        world.Rooms.Add(room.Id, room);

        var player = CreatePlayer();
        room.PlayersInRoom.Add(player);

        var item = new Object
        {
            Name = "Lamp",
            Description = "A shiny lamp"
        };

        item.MoveTo(room);

        var handler = new TakeHandler(world);

        var result = await handler.Handle(new TakeCommand
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
        var world = CreateWorld();
        var room = CreateRoom("room");
        world.Rooms.Add(room.Id, room);

        var player = CreatePlayer();
        room.PlayersInRoom.Add(player);

        var handler = new TakeHandler(world);

        var result = await handler.Handle(new TakeCommand
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
        var world = CreateWorld();
        var room = CreateRoom("room");
        world.Rooms.Add(room.Id, room);

        var owner = CreatePlayer("Owner");
        var seeker = CreatePlayer("Seeker");
        room.PlayersInRoom.Add(owner);
        room.PlayersInRoom.Add(seeker);

        var item = new Object
        {
            Name = "Gem",
            Description = "A bright gem"
        };

        item.MoveTo(owner);

        var handler = new TakeHandler(world);

        var result = await handler.Handle(new TakeCommand
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
        var world = CreateWorld();
        var room = CreateRoom("room");
        world.Rooms.Add(room.Id, room);

        var player = CreatePlayer();
        room.PlayersInRoom.Add(player);

        var item = new Object
        {
            Name = "Gem",
            Description = "A bright gem"
        };

        item.MoveTo(player);

        var handler = new TakeHandler(world);

        var result = await handler.Handle(new TakeCommand
        {
            Player = player,
            Target = "Gem"
        });

        var message = Assert.Single(result.Messages);
        Assert.IsType<ItemAlreadyInPossessionEvent>(message.Event);
    }

    [Fact]
    public async Task ExamineHandler_ReturnsRoomDescriptionWhenNoTarget()
    {
        var world = CreateWorld();
        var room = CreateRoom("room");
        world.Rooms.Add(room.Id, room);

        var player = CreatePlayer();
        room.PlayersInRoom.Add(player);

        var handler = new ExamineHandler(world);

        var result = await handler.Handle(new ExamineCommand
        {
            Player = player,
            Target = string.Empty
        });

        var message = Assert.Single(result.Messages);
        var evt = Assert.IsType<RoomDescriptionEvent>(message.Event);
        Assert.Equal(room.DescribeFor(player, useLongDescription: true), evt.Description);
    }

    [Fact]
    public async Task ExamineHandler_ReturnsSelfInventoryWhenTargetIsMe()
    {
        var world = CreateWorld();
        var room = CreateRoom("room");
        world.Rooms.Add(room.Id, room);

        var player = CreatePlayer();
        room.PlayersInRoom.Add(player);

        var item = new Object
        {
            Name = "Lantern",
            Description = "An old lantern"
        };

        item.MoveTo(player);

        var handler = new ExamineHandler(world);

        var result = await handler.Handle(new ExamineCommand
        {
            Player = player,
            Target = "me"
        });

        var message = Assert.Single(result.Messages);
        var evt = Assert.IsType<SelfExaminedEvent>(message.Event);
        Assert.Contains(item, evt.Inventory);
    }

    [Fact]
    public async Task ExamineHandler_ReturnsObjectDetailsWhenFound()
    {
        var world = CreateWorld();
        var room = CreateRoom("room");
        world.Rooms.Add(room.Id, room);

        var player = CreatePlayer();
        room.PlayersInRoom.Add(player);

        var item = new Object
        {
            Name = "Scroll",
            Description = "A dusty scroll"
        };

        item.MoveTo(room);

        var handler = new ExamineHandler(world);

        var result = await handler.Handle(new ExamineCommand
        {
            Player = player,
            Target = "Scroll"
        });

        var message = Assert.Single(result.Messages);
        var evt = Assert.IsType<ObjectExaminedEvent>(message.Event);
        Assert.Same(item, evt.Item);
    }

    [Fact]
    public async Task SayHandler_BroadcastsToRoom()
    {
        var world = CreateWorld();
        var room = CreateRoom("room");
        world.Rooms.Add(room.Id, room);

        var speaker = CreatePlayer("Speaker");
        var listener = CreatePlayer("Listener");
        room.PlayersInRoom.AddRange([speaker, listener]);

        var handler = new SayHandler(world);

        var result = await handler.Handle(new SayCommand
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
        var world = CreateWorld();
        var room = CreateRoom("room");
        world.Rooms.Add(room.Id, room);

        var speaker = CreatePlayer();
        room.PlayersInRoom.Add(speaker);

        var handler = new SayHandler(world);

        var result = await handler.Handle(new SayCommand
        {
            Player = speaker,
            Message = "   "
        });

        var message = Assert.Single(result.Messages);
        var evt = Assert.IsType<SystemMessageEvent>(message.Event);
        Assert.False(string.IsNullOrWhiteSpace(evt.Message));
    }

    private static World CreateWorld()
    {
        var options = Options.Create(new AppOptions
        {
            PlayerDatabaseFilepath = "players.db",
            WorldDataFilepath = "world.json"
        });

        return new World(options, NullLogger<World>.Instance);
    }

    private static Room CreateRoom(string slug)
    {
        return new Room
        {
            Id = slug,
            Name = $"{slug} name",
            Description = $"{slug} description",
            LongDescription = $"{slug} long description",
            EnterText = "",
            ExitText = ""
        };
    }

    private static Player CreatePlayer(string? username = null)
    {
        return new Player
        {
            Username = username ?? "Player",
            Connection = new TestPlayerConnection()
        };
    }

    private sealed class TestPlayerConnection : IPlayerConnection
    {
        public string Id { get; } = Guid.NewGuid().ToString();

        public Task SendMessageAsync(string message) => Task.CompletedTask;
    }
}
