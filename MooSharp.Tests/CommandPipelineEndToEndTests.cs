using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MooSharp.Actors.Players;
using MooSharp.Actors.Rooms;
using MooSharp.Commands.Commands;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;
using MooSharp.Commands.Searching;
using MooSharp.Features.Editor;
using MooSharp.Game;
using MooSharp.Infrastructure.Messaging;
using MooSharp.Scripting;
using MooSharp.Tests.Handlers;
using MooSharp.Tests.TestDoubles;
using NSubstitute;

namespace MooSharp.Tests;

public class CommandPipelineEndToEndTests
{
    private readonly World.World _world;
    private readonly Player _player;
    private readonly Room _origin;
    private readonly Room _destination;
    private readonly IGameMessageEmitter _emitter = Substitute.For<IGameMessageEmitter>();
    private readonly GameInputProcessor _inputProcessor;

    public CommandPipelineEndToEndTests()
    {
        _origin = HandlerTestHelpers.CreateRoom("origin");
        _destination = HandlerTestHelpers.CreateRoom("destination");
        _origin.Exits.Add(new()
        {
            Name = "north",
            Description = "",
            Destination = _destination.Id
        });

        var repository = new InMemoryWorldRepository();
        _world = new(repository, NullLogger<World.World>.Instance);
        _world.Initialize([_origin, _destination]);

        _player = HandlerTestHelpers.CreatePlayer("Tester");
        _world.RegisterPlayer(_player);
        _world.MovePlayer(_player, _origin);

        var resolver = new TargetResolver();
        var binder = new ArgumentBinder(resolver, _world);

        var definitions = new ICommandDefinition[]
        {
            new MoveCommandDefinition()
        };

        var parser = new CommandParser(NullLogger<CommandParser>.Instance, definitions, _world, binder);
        var services = new ServiceCollection();
        services.AddSingleton(_world);
        services.AddSingleton<IHandler<MoveCommand>>(_ => new MoveHandler(_world, NullLogger<MoveHandler>.Instance));

        var provider = services.BuildServiceProvider();
        var executor = new CommandExecutor(provider, NullLogger<CommandExecutor>.Instance);

        var verbResolver = new VerbScriptResolver();
        var editorModeService = Substitute.For<IEditorModeService>();
        var editorModeHandler = Substitute.For<IEditorModeHandler>();
        _inputProcessor = new(_world, parser, executor, _emitter, verbResolver,
            editorModeService, editorModeHandler,
            NullLogger<GameInputProcessor>.Instance);
    }

    [Fact]
    public async Task ProcessInputAsync_ExecutesParsedCommandThroughHandler()
    {
        var command = new InputCommand(_player.Id.Value, "move north");

        await _inputProcessor.ProcessInputAsync(command);

        Assert.Same(_destination, _world.GetLocationOrThrow(_player));

        await _emitter.Received(1).SendGameMessagesAsync(
            Arg.Is<IEnumerable<GameMessage>>(messages => ContainsMoveAndDescription(messages, _destination)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessInputAsync_SendsSystemMessageForUnknownVerb()
    {
        var startingLocation = _world.GetLocationOrThrow(_player);

        await _inputProcessor.ProcessInputAsync(new(_player.Id.Value, "dance"));

        Assert.Same(startingLocation, _world.GetLocationOrThrow(_player));

        await _emitter.Received(1).SendGameMessagesAsync(
            Arg.Is<IEnumerable<GameMessage>>(messages => AllSystemMessages(messages)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessInputAsync_SendsSystemMessageWhenArgumentsAreMissing()
    {
        var startingLocation = _world.GetLocationOrThrow(_player);

        await _inputProcessor.ProcessInputAsync(new(_player.Id.Value, "move"));

        Assert.Same(startingLocation, _world.GetLocationOrThrow(_player));

        await _emitter.Received(1).SendGameMessagesAsync(
            Arg.Is<IEnumerable<GameMessage>>(messages => AllSystemMessages(messages)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessInputAsync_ProcessesQueuedCommands()
    {
        var follower = HandlerTestHelpers.CreatePlayer("Follower");
        follower.FollowTarget = _player.Id;

        _world.RegisterPlayer(follower);
        _world.MovePlayer(follower, _origin);

        var command = new InputCommand(_player.Id.Value, "move north");

        await _inputProcessor.ProcessInputAsync(command);

        Assert.Same(_destination, _world.GetLocationOrThrow(follower));

        await _emitter.Received(1).SendGameMessagesAsync(
            Arg.Is<IEnumerable<GameMessage>>(messages => ContainsMoveForPlayer(messages, _player, _destination)),
            Arg.Any<CancellationToken>());

        await _emitter.Received(1).SendGameMessagesAsync(
            Arg.Is<IEnumerable<GameMessage>>(messages => ContainsMoveForPlayer(messages, follower, _destination)),
            Arg.Any<CancellationToken>());
    }

    private static bool ContainsMoveAndDescription(IEnumerable<GameMessage> messages, Room destination)
    {
        var list = messages.ToList();

        return list.Any(m => m.Event is PlayerMovedEvent moved && ReferenceEquals(moved.Destination, destination)) &&
               list.Any(m => m.Event is RoomDescriptionEvent);
    }

    private static bool AllSystemMessages(IEnumerable<GameMessage> messages)
    {
        return messages.All(m => m.Event is SystemMessageEvent);
    }

    private static bool ContainsMoveForPlayer(IEnumerable<GameMessage> messages, Player player, Room destination)
    {
        return messages.Any(message => message.Event is PlayerMovedEvent moved &&
                                       ReferenceEquals(moved.Player, player) &&
                                       ReferenceEquals(moved.Destination, destination));
    }
}
