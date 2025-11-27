using Microsoft.Extensions.Logging.Abstractions;
using MooSharp;
using MooSharp.Messaging;
using MooSharp.Tests.TestDoubles;

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
    public async Task ChannelHandler_BroadcastsAcrossRooms()
    {
        var roomOne = CreateRoom("one");
        var roomTwo = CreateRoom("two");
        var world = await CreateWorld(roomOne, roomTwo);

        var speaker = CreatePlayer("Speaker");
        var listener = CreatePlayer("Listener");
        var localListener = CreatePlayer("LocalListener");

        world.MovePlayer(speaker, roomOne);
        world.MovePlayer(listener, roomTwo);
        world.MovePlayer(localListener, roomOne);

        var handler = new ChannelHandler(world);

        var result = await handler.Handle(new ChannelCommand
        {
            Player = speaker,
            Channel = ChatChannels.Global,
            Message = "Hello all"
        });

        var actorMessage = Assert.Single(result.Messages, m => m.Player == speaker);
        Assert.Equal(MessageAudience.Actor, actorMessage.Audience);
        Assert.IsType<ChannelMessageEvent>(actorMessage.Event);

        var listenerMessages = result.Messages.Where(m => m.Player == listener || m.Player == localListener).ToList();
        Assert.Equal(2, listenerMessages.Count);

        foreach (var message in listenerMessages)
        {
            Assert.Equal(MessageAudience.Observer, message.Audience);
            Assert.IsType<ChannelMessageEvent>(message.Event);
        }
    }

    [Fact]
    public async Task ChannelHandler_RespectsMutedRecipients()
    {
        var room = CreateRoom("room");
        var world = await CreateWorld(room);

        var speaker = CreatePlayer("Speaker");
        var mutedListener = CreatePlayer("Muted");
        var listener = CreatePlayer("Listener");

        mutedListener.MuteChannel(ChatChannels.Global);

        world.MovePlayer(speaker, room);
        world.MovePlayer(mutedListener, room);
        world.MovePlayer(listener, room);

        var handler = new ChannelHandler(world);

        var result = await handler.Handle(new ChannelCommand
        {
            Player = speaker,
            Channel = ChatChannels.Global,
            Message = "Hello"
        });

        Assert.DoesNotContain(result.Messages, m => m.Player == mutedListener && m.Event is ChannelMessageEvent);
        Assert.Contains(result.Messages, m => m.Player == listener && m.Event is ChannelMessageEvent);
    }

    [Fact]
    public async Task ChannelMuteHandler_TogglesMuteState()
    {
        var player = CreatePlayer();
        var handler = new ChannelMuteHandler();

        var muteResult = await handler.Handle(new ChannelMuteCommand
        {
            Player = player,
            Channel = ChatChannels.Global,
            Mute = true
        });

        Assert.True(player.IsChannelMuted(ChatChannels.Global));
        var muteMessage = Assert.Single(muteResult.Messages);
        Assert.IsType<SystemMessageEvent>(muteMessage.Event);

        var unmuteResult = await handler.Handle(new ChannelMuteCommand
        {
            Player = player,
            Channel = ChatChannels.Global,
            Mute = false
        });

        Assert.False(player.IsChannelMuted(ChatChannels.Global));
        var unmuteMessage = Assert.Single(unmuteResult.Messages);
        Assert.IsType<SystemMessageEvent>(unmuteMessage.Event);
    }

    private static Task<World> CreateWorld(params Room[] rooms)
    {
        var store = new InMemoryWorldStore();
        var world = new World(store, NullLogger<World>.Instance);

        world.Initialize(rooms);

        return Task.FromResult(world);
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
}
