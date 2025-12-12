using System.Collections.Generic;
using MooSharp.Actors.Rooms;
using MooSharp.Commands.Commands.Informational;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Searching;
using MooSharp.Tests.Handlers;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Tests.Commands.Informational;

public class ExamineCommandDefinitionTests
{
    [Fact]
    public async Task TryCreateCommand_ReturnsRoomCommandWhenNoTarget()
    {
        var (definition, binder, context, _) = await CreateContextAsync();

        var error = definition.TryCreateCommand(context, binder, out var command);

        Assert.Null(error);
        Assert.IsType<ExamineRoomCommand>(command);
    }

    [Fact]
    public async Task TryCreateCommand_ReturnsSelfCommand()
    {
        var (definition, binder, context, _) = await CreateContextAsync("me");

        var error = definition.TryCreateCommand(context, binder, out var command);

        Assert.Null(error);
        Assert.IsType<ExamineSelfCommand>(command);
    }

    [Fact]
    public async Task TryCreateCommand_ReturnsPlayerCommand()
    {
        var (definition, binder, context, world) = await CreateContextAsync("Friend");
        var player = context.Player;
        var room = context.Room;

        var friend = HandlerTestHelpers.CreatePlayer("Friend");
        world.MovePlayer(friend, room);

        var error = definition.TryCreateCommand(context, binder, out var command);

        Assert.Null(error);
        var examine = Assert.IsType<ExaminePlayerCommand>(command);
        Assert.Same(friend, examine.Target);
        Assert.Same(player, examine.Player);
    }

    [Fact]
    public async Task TryCreateCommand_ReturnsObjectCommand()
    {
        var (definition, binder, context, _) = await CreateContextAsync("Scroll");
        var room = context.Room;

        var item = new Object
        {
            Name = "Scroll",
            Description = "A dusty scroll"
        };

        item.MoveTo(room);

        var error = definition.TryCreateCommand(context, binder, out var command);

        Assert.Null(error);
        var examine = Assert.IsType<ExamineObjectCommand>(command);
        Assert.Same(item, examine.Target);
    }

    [Fact]
    public async Task TryCreateCommand_ReturnsExitCommand()
    {
        var (definition, binder, context, _) = await CreateContextAsync("north");
        var room = context.Room;
        var destination = HandlerTestHelpers.CreateRoom("destination");
        var exit = new Exit
        {
            Name = "north",
            Description = "A doorway",
            Destination = destination.Id
        };

        room.Exits.Add(exit);

        var error = definition.TryCreateCommand(context, binder, out var command);

        Assert.Null(error);
        var examine = Assert.IsType<ExamineExitCommand>(command);
        Assert.Same(exit, examine.Target);
    }

    [Fact]
    public async Task TryCreateCommand_ReturnsErrorForAmbiguousObject()
    {
        var itemOne = new Object
        {
            Name = "Rock",
            Description = "A heavy rock"
        };

        var itemTwo = new Object
        {
            Name = "Rock",
            Description = "A lighter rock"
        };

        var (definition, binder, context, _) = await CreateContextAsync("rock");

        itemOne.MoveTo(context.Room);
        itemTwo.MoveTo(context.Room);

        var error = definition.TryCreateCommand(context, binder, out var command);

        Assert.NotNull(error);
        Assert.Null(command);
    }

    [Fact]
    public async Task TryCreateCommand_ReturnsErrorForMissingItem()
    {
        var (definition, binder, context, _) = await CreateContextAsync("missing");

        var error = definition.TryCreateCommand(context, binder, out var command);

        Assert.NotNull(error);
        Assert.Null(command);
    }

    private static async Task<(ExamineCommandDefinition definition, ArgumentBinder binder, ParsingContext context, World.World world)> CreateContextAsync(params string[] tokens)
    {
        var resolver = new TargetResolver();
        var (world, player, room) = await CreateWorldAsync();
        var binder = new ArgumentBinder(resolver, world);
        var tokenQueue = new Queue<string>(tokens);
        var context = new ParsingContext(player, room, tokenQueue);

        return (new ExamineCommandDefinition(), binder, context, world);
    }

    private static async Task<(World.World world, MooSharp.Actors.Players.Player player, Room room)> CreateWorldAsync()
    {
        var room = HandlerTestHelpers.CreateRoom("room");
        var world = await HandlerTestHelpers.CreateWorld(room);

        var player = HandlerTestHelpers.CreatePlayer();
        world.MovePlayer(player, room);

        return (world, player, room);
    }
}
