using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MooSharp;
using MooSharp.Messaging;
using MooSharp.Persistence;

namespace MooSharp.Tests;

public class CommandHandlerTests
{
    [Fact]
    public async Task MoveHandler_MovesPlayerAndAddsMovementEvents()
    {
        var origin = CreateRoom("origin");
        var destination = CreateRoom("destination");
        origin.Exits.Add("north", destination.Id);

        var world = await CreateWorld(origin, destination);

        var player = CreatePlayer("Alice");
        world.MovePlayer(player, origin);

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
        var origin = CreateRoom("origin");
        var world = await CreateWorld(origin);

        var player = CreatePlayer();
        world.MovePlayer(player, origin);

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
        var origin = CreateRoom("origin");
        var destination = CreateRoom("destination");
        origin.Exits.Add("north", destination.Id);

        var world = await CreateWorld(origin, destination);

        var actor = CreatePlayer("Actor");
        var originObserver = CreatePlayer("OriginObserver");
        var destinationObserver = CreatePlayer("DestinationObserver");
        
        world.MovePlayer(actor, origin);
        world.MovePlayer(originObserver, origin);
        world.MovePlayer(destinationObserver, destination);
        
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
        var room = CreateRoom("room");
        var world = await CreateWorld(room);

        var player = CreatePlayer();
        world.MovePlayer(player, room);

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
        var room = CreateRoom("room");
        var world = await CreateWorld(room);

        var player = CreatePlayer();
        world.MovePlayer(player, room);

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
        var room = CreateRoom("room");
        var world = await CreateWorld(room);

        var owner = CreatePlayer("Owner");
        var seeker = CreatePlayer("Seeker");
        world.MovePlayer(owner, room);
        world.MovePlayer(seeker, room);

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
        var room = CreateRoom("room");
        var world = await CreateWorld(room);

        var player = CreatePlayer();
        world.MovePlayer(player, room);

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
        var room = CreateRoom("room");
        var world = await CreateWorld(room);

        var player = CreatePlayer();
        world.MovePlayer(player, room);

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
        var room = CreateRoom("room");
        var world = await CreateWorld(room);

        var player = CreatePlayer();
        world.MovePlayer(player, room);

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
        var room = CreateRoom("room");
        var world = await CreateWorld(room);

        var player = CreatePlayer();
        world.MovePlayer(player, room);

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
        var room = CreateRoom("room");
        var world = await CreateWorld(room);

        var speaker = CreatePlayer("Speaker");
        var listener = CreatePlayer("Listener");
        world.MovePlayer(speaker, room);
        world.MovePlayer(listener, room);
        
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
        var room = CreateRoom("room");
        var world = await CreateWorld(room);

        var speaker = CreatePlayer();
        world.MovePlayer(speaker, room);

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

    [Fact]
    public async Task DigHandler_CreatesRoomAndPersistsExits()
    {
        var origin = CreateRoom("origin");
        var (world, store) = await CreateWorldWithStore(origin);

        var builder = CreatePlayer("Builder");
        world.MovePlayer(builder, origin);

        var handler = new DigHandler(world, new SlugCreator());

        var result = await handler.Handle(new DigCommand
        {
            Player = builder,
            RoomName = "New Wing"
        });

        Assert.Contains(result.Messages, m => m.Event is SystemMessageEvent);

        var newRoom = world.Rooms.Values.Single(r => r.Name == "New Wing");
        Assert.Equal(newRoom.Id, origin.Exits[newRoom.Id.Value]);
        Assert.Equal(origin.Id, newRoom.Exits[origin.Id.Value]);

        var persistedRooms = await store.LoadRoomsAsync();
        var persistedNewRoom = persistedRooms.Single(r => r.Id == newRoom.Id);

        Assert.Equal(origin.Id, persistedNewRoom.Exits[origin.Id.Value]);
    }

    [Fact]
    public async Task DigHandler_RejectsDuplicateExitSlugAcrossWorld()
    {
        var origin = CreateRoom("origin");
        var existingDestination = CreateRoom("treasure-room");
        origin.Exits[existingDestination.Id.Value] = existingDestination.Id;

        var world = await CreateWorld(origin, existingDestination);

        var builder = CreatePlayer("Builder");
        world.MovePlayer(builder, origin);

        var handler = new DigHandler(world, new SlugCreator());

        var result = await handler.Handle(new DigCommand
        {
            Player = builder,
            RoomName = "Treasure Room"
        });

        var message = Assert.Single(result.Messages);
        var systemMessage = Assert.IsType<SystemMessageEvent>(message.Event);
        Assert.Contains("slug 'treasure-room' already exists", systemMessage.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static WorldFactory CreateWorldFactory(InMemoryWorldStore? store = null)
    {
        var options = Options.Create(new AppOptions
        {
            WorldDataFilepath = "world.json",
            DatabaseFilepath = "game.db"
        });

        return new WorldFactory(options, NullLogger<WorldFactory>.Instance, store ?? new InMemoryWorldStore(),
            NullLogger<World>.Instance);
    }

    private static Task<World> CreateWorld(params Room[] rooms) => CreateWorld(null, rooms);

    private static async Task<World> CreateWorld(InMemoryWorldStore? store, params Room[] rooms)
    {
        var factory = CreateWorldFactory(store);

        return await factory.CreateWorldAsync(rooms.ToList());
    }

    private static async Task<(World world, InMemoryWorldStore store)> CreateWorldWithStore(params Room[] rooms)
    {
        var store = new InMemoryWorldStore();
        var world = await CreateWorld(store, rooms);

        return (world, store);
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

    private sealed class InMemoryWorldStore : IWorldStore
    {
        private readonly List<Room> _rooms = new();
        private readonly List<(RoomId From, RoomId To, string Direction)> _exits = new();

        public Task<bool> HasRoomsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_rooms.Any());

        public Task<IReadOnlyCollection<Room>> LoadRoomsAsync(CancellationToken cancellationToken = default)
        {
            var rooms = _rooms.Select(CloneRoom).ToList();

            foreach (var exit in _exits)
            {
                var origin = rooms.SingleOrDefault(r => r.Id == exit.From);
                if (origin is null)
                {
                    continue;
                }

                origin.Exits[exit.Direction] = exit.To;
            }

            return Task.FromResult<IReadOnlyCollection<Room>>(rooms);
        }

        public Task SaveRoomAsync(Room room, CancellationToken cancellationToken = default)
        {
            _rooms.RemoveAll(r => r.Id == room.Id);
            _rooms.Add(CloneRoom(room));
            return Task.CompletedTask;
        }

        public Task SaveExitAsync(RoomId fromRoomId, RoomId toRoomId, string direction,
            CancellationToken cancellationToken = default)
        {
            _exits.RemoveAll(e => e.From == fromRoomId && string.Equals(e.Direction, direction, StringComparison.OrdinalIgnoreCase));
            _exits.Add((fromRoomId, toRoomId, direction));
            return Task.CompletedTask;
        }

        public Task SaveRoomsAsync(IEnumerable<Room> rooms, CancellationToken cancellationToken = default)
        {
            _rooms.Clear();
            _rooms.AddRange(rooms.Select(CloneRoom));

            _exits.Clear();

            foreach (var room in rooms)
            {
                foreach (var exit in room.Exits)
                {
                    _exits.Add((room.Id, exit.Value, exit.Key));
                }
            }

            return Task.CompletedTask;
        }

        private static Room CloneRoom(Room room)
        {
            var clone = new Room
            {
                Id = room.Id,
                Name = room.Name,
                Description = room.Description,
                LongDescription = room.LongDescription,
                EnterText = room.EnterText,
                ExitText = room.ExitText
            };

            foreach (var exit in room.Exits)
            {
                clone.Exits[exit.Key] = exit.Value;
            }

            return clone;
        }
    }
}
