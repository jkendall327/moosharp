using Microsoft.Extensions.Logging.Abstractions;
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
        var definition = new RecordingDefinition("known");
        var parser = new CommandParser(NullLogger<CommandParser>.Instance, new[] { definition });
        var player = CreatePlayer();

        var result = await parser.ParseAsync(player, "unknown args");

        Assert.Null(result);
        Assert.Null(definition.LastArgs);
    }

    [Fact]
    public async Task ParseAsync_DoesNotMatchMultiWordVerbs()
    {
        var definition = new RecordingDefinition("look at");
        var parser = new CommandParser(NullLogger<CommandParser>.Instance, new[] { definition });
        var player = CreatePlayer();

        var result = await parser.ParseAsync(player, "look at shiny sword");

        Assert.Null(result);
        Assert.Null(definition.LastArgs);
    }

    [Fact]
    public async Task ParseAsync_MatchesVerbCaseInsensitivelyAndPassesNormalizedArgs()
    {
        var definition = new RecordingDefinition("say");
        var parser = new CommandParser(NullLogger<CommandParser>.Instance, new[] { definition });
        var player = CreatePlayer();

        var result = await parser.ParseAsync(player, "  sAy   spaced   words   ");

        var command = Assert.IsType<StubCommand>(result);
        Assert.Equal("spaced words", command.Args);
        Assert.Equal(player, definition.LastPlayer);
        Assert.Equal("spaced words", definition.LastArgs);
    }

    private static Player CreatePlayer()
    {
        return new Player
        {
            Username = "Player",
            Connection = new TestPlayerConnection()
        };
    }
}
