using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using MooSharp;

namespace MooSharp.Tests;

public class CommandParserTests
{
    [Fact]
    public async Task ParseAsync_ReturnsNullForEmptyOrWhitespaceInput()
    {
        var parser = new CommandParser(NullLogger<CommandParser>.Instance, Enumerable.Empty<ICommandDefinition>());
        var player = CreatePlayer();

        var result = await parser.ParseAsync(player, "   \t  ");

        Assert.Null(result);
    }

    [Fact]
    public async Task ParseAsync_ReturnsNullForUnknownVerbEvenWhenDefinitionsExist()
    {
        var definition = CreateDefinition("known");
        var parser = new CommandParser(NullLogger<CommandParser>.Instance, new[] { definition });
        var player = CreatePlayer();

        var result = await parser.ParseAsync(player, "unknown args");

        Assert.Null(result);
        definition.DidNotReceiveWithAnyArgs().Create(default!, default!);
    }

    [Fact]
    public async Task ParseAsync_DoesNotMatchMultiWordVerbs()
    {
        var definition = CreateDefinition("look at");
        var parser = new CommandParser(NullLogger<CommandParser>.Instance, new[] { definition });
        var player = CreatePlayer();

        var result = await parser.ParseAsync(player, "look at shiny sword");

        Assert.Null(result);
        definition.DidNotReceiveWithAnyArgs().Create(default!, default!);
    }

    [Fact]
    public async Task ParseAsync_MatchesVerbCaseInsensitivelyAndPassesNormalizedArgs()
    {
        var definition = CreateDefinition("say");
        var parser = new CommandParser(NullLogger<CommandParser>.Instance, new[] { definition });
        var player = CreatePlayer();

        var result = await parser.ParseAsync(player, "  sAy   spaced   words   ");

        var command = Assert.IsType<StubCommand>(result);
        Assert.Equal("spaced words", command.Args);
        definition.Received(1).Create(player, "spaced words");
    }

    private static Player CreatePlayer()
    {
        return new Player
        {
            Username = "Player",
            Connection = new TestPlayerConnection()
        };
    }

    private static ICommandDefinition CreateDefinition(string verb)
    {
        var definition = Substitute.For<ICommandDefinition>();
        definition.Verbs.Returns(new[] { verb });
        definition.Description.Returns("test command");
        definition.Create(Arg.Any<Player>(), Arg.Any<string>())
            .Returns(call => new StubCommand(call.ArgAt<string>(1)));

        return definition;
    }
}
