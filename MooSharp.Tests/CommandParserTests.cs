using Microsoft.Extensions.Logging.Abstractions;
using MooSharp.Actors;
using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;
using MooSharp.Tests.TestDoubles;
using NSubstitute;

namespace MooSharp.Tests;

public class CommandParserTests
{
    [Fact]
    public async Task ParseAsync_ReturnsNullForEmptyOrWhitespaceInput()
    {
        var parser = new CommandParser(NullLogger<CommandParser>.Instance, []);
        var player = CreatePlayer();

        var result = await parser.ParseAsync(player, "   \t  ");

        Assert.Null(result);
    }

    [Fact]
    public async Task ParseAsync_ReturnsNullForUnknownVerbEvenWhenDefinitionsExist()
    {
        var definition = CreateDefinition("known");
        var parser = new CommandParser(NullLogger<CommandParser>.Instance, [definition]);
        var player = CreatePlayer();

        var result = await parser.ParseAsync(player, "unknown args");

        Assert.Null(result);

        definition
            .DidNotReceiveWithAnyArgs()
            .Create(default!, default!);
    }

    [Fact]
    public async Task ParseAsync_DoesNotMatchMultiWordVerbs()
    {
        var definition = CreateDefinition("look at");
        var parser = new CommandParser(NullLogger<CommandParser>.Instance, [definition]);
        var player = CreatePlayer();

        var result = await parser.ParseAsync(player, "look at shiny sword");

        Assert.Null(result);

        definition
            .DidNotReceiveWithAnyArgs()
            .Create(default!, default!);
    }

    [Fact]
    public async Task ParseAsync_MatchesVerbCaseInsensitivelyAndPassesNormalizedArgs()
    {
        var definition = CreateDefinition("say");
        var parser = new CommandParser(NullLogger<CommandParser>.Instance, [definition]);
        var player = CreatePlayer();

        var result = await parser.ParseAsync(player, "  sAy   spaced   words   ");

        var command = Assert.IsType<StubCommand>(result);
        Assert.Equal("spaced words", command.Args);

        definition
            .Received(1)
            .Create(player, "spaced words");
    }

    [Fact]
    public async Task ParseAsync_MapsQuotePrefixToSayCommand()
    {
        var definition = CreateDefinition("say", "s");
        var parser = new CommandParser(NullLogger<CommandParser>.Instance, [definition]);
        var player = CreatePlayer();

        var result = await parser.ParseAsync(player, "\"   hello   world  ");

        var command = Assert.IsType<StubCommand>(result);
        Assert.Equal("hello world", command.Args);

        definition
            .Received(1)
            .Create(player, "hello world");
    }

    [Fact]
    public async Task ParseAsync_MapsColonPrefixToEmoteCommand()
    {
        var definition = CreateDefinition("/me");
        var parser = new CommandParser(NullLogger<CommandParser>.Instance, [definition]);
        var player = CreatePlayer();

        var result = await parser.ParseAsync(player, ":  waves  excitedly ");

        var command = Assert.IsType<StubCommand>(result);
        Assert.Equal("waves excitedly", command.Args);

        definition
            .Received(1)
            .Create(player, "waves excitedly");
    }

    private static Player CreatePlayer()
    {
        return new()
        {
            Id = PlayerId.New(),
            Username = "Player"
        };
    }

    private static ICommandDefinition CreateDefinition(params string[] verbs)
    {
        var definition = Substitute.For<ICommandDefinition>();
        definition.Verbs.Returns(verbs);
        definition.Description.Returns("test command");

        definition
            .Create(Arg.Any<Player>(), Arg.Any<string>())
            .Returns(call => new StubCommand(call.ArgAt<string>(1)));

        return definition;
    }
}