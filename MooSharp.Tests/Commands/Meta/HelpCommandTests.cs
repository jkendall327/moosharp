using Microsoft.Extensions.Logging;
using MooSharp.Actors.Players;
using MooSharp.Actors.Rooms;
using MooSharp.Commands.Commands.Meta;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;
using MooSharp.Commands.Searching;
using MooSharp.Data.Worlds;
using NSubstitute;
using Xunit;

namespace MooSharp.Tests.Commands.Meta;

public class HelpCommandTests
{
    private readonly Player _player = new()
    {
        Id = PlayerId.New(),
        Username = "TestUser"
    };

    private MooSharp.World.World CreateMockWorld()
    {
        return Substitute.For<MooSharp.World.World>(
            Substitute.For<IWorldRepository>(),
            Substitute.For<ILogger<MooSharp.World.World>>());
    }

    [Fact]
    public void Definition_Parses_NoArgs()
    {
        var definition = new HelpCommandDefinition();
        var ctx = new ParsingContext(
            _player,
            Substitute.For<Room>(),
            new Queue<string>([]));

        var binder = Substitute.For<ArgumentBinder>(new TargetResolver(), CreateMockWorld());

        var result = definition.TryCreateCommand(ctx, binder, out var command);

        Assert.Null(result);
        Assert.NotNull(command);
        var helpCmd = Assert.IsType<HelpCommand>(command);
        Assert.Equal(_player, helpCmd.Player);
        Assert.Null(helpCmd.Topic);
    }

    [Fact]
    public void Definition_Parses_Topic()
    {
        var definition = new HelpCommandDefinition();
        var ctx = new ParsingContext(
            _player,
            Substitute.For<Room>(),
            new Queue<string>(["look"]));

        var binder = Substitute.For<ArgumentBinder>(new TargetResolver(), CreateMockWorld());

        var result = definition.TryCreateCommand(ctx, binder, out var command);

        Assert.Null(result);
        Assert.NotNull(command);
        var helpCmd = Assert.IsType<HelpCommand>(command);
        Assert.Equal("look", helpCmd.Topic);
    }

    [Fact]
    public async Task Handler_GeneralHelp_WhenNoTopic()
    {
        var commandRef = Substitute.For<CommandReference>(Enumerable.Empty<ICommandDefinition>());
        commandRef.BuildHelpText().Returns("General Help Text");

        var handler = new HelpHandler(commandRef);
        var cmd = new HelpCommand { Player = _player, Topic = null };

        var result = await handler.Handle(cmd);

        var message = Assert.Single(result.Messages);
        Assert.Equal(_player, message.Player);
        var sysMsg = Assert.IsType<SystemMessageEvent>(message.Event);
        Assert.Equal("General Help Text", sysMsg.Message);
    }

    [Fact]
    public async Task Handler_SpecificHelp_WhenTopicFound()
    {
        var commandRef = Substitute.For<CommandReference>(Enumerable.Empty<ICommandDefinition>());
        commandRef.GetHelpForCommand("look").Returns("Help for Look");

        var handler = new HelpHandler(commandRef);
        var cmd = new HelpCommand { Player = _player, Topic = "look" };

        var result = await handler.Handle(cmd);

        var message = Assert.Single(result.Messages);
        Assert.Equal(_player, message.Player);
        var sysMsg = Assert.IsType<SystemMessageEvent>(message.Event);
        Assert.Equal("Help for Look", sysMsg.Message);
    }

    [Fact]
    public async Task Handler_NotFound_WhenTopicMissing()
    {
        var commandRef = Substitute.For<CommandReference>(Enumerable.Empty<ICommandDefinition>());
        commandRef.GetHelpForCommand("unknown").Returns((string?)null);

        var handler = new HelpHandler(commandRef);
        var cmd = new HelpCommand { Player = _player, Topic = "unknown" };

        var result = await handler.Handle(cmd);

        var message = Assert.Single(result.Messages);
        Assert.Equal(_player, message.Player);
        var sysMsg = Assert.IsType<SystemMessageEvent>(message.Event);
        Assert.Equal("Command not found.", sysMsg.Message);
    }
}
