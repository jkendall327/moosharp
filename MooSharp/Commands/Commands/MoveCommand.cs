using Microsoft.Extensions.Logging;
using MooSharp.Actors.Players;
using MooSharp.Actors.Rooms;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;

namespace MooSharp.Commands.Commands;

public class MoveCommand : CommandBase<MoveCommand>
{
    public required Player Player { get; init; }
    public required Exit TargetExit { get; init; }
}

public class MoveCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["move", "m", "go", "walk"];

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        command = null;

        var roomResult = binder.BindExitInRoom(ctx);

        if (roomResult.IsSuccess)
        {
            command = new MoveCommand
            {
                Player = ctx.Player,
                TargetExit = roomResult.Value!
            };

            return null;
        }

        return roomResult.ErrorMessage;
    }

    public CommandCategory Category => CommandCategory.General;

    public string Description => "Move to an adjacent room. Usage: move <exit>.";
}

public class MoveHandler(World.World world, ILogger<MoveHandler> logger) : IHandler<MoveCommand>
{
    public Task<CommandResult> Handle(MoveCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var player = cmd.Player;

        var originRoom = world.GetLocationOrThrow(player);

        var exit = cmd.TargetExit;

        if (exit.IsLocked && !exit.IsOpen)
        {
            result.Add(player, new SystemMessageEvent("The door is locked."));
            return Task.FromResult(result);
        }

        if (!exit.IsOpen)
        {
            result.Add(player, new SystemMessageEvent("The door is closed."));
            return Task.FromResult(result);
        }

        var exitRoom = world.Rooms[exit.Destination];

        result.Add(player, new PlayerDepartedEvent(player, originRoom, cmd.TargetExit.Name));
        result.Add(player, new PlayerMovedEvent(player, exitRoom));

        result.BroadcastToAllButPlayer(originRoom, player, new PlayerDepartedEvent(player, originRoom, cmd.TargetExit.Name));

        world.MovePlayer(player, exitRoom);

        var description = exitRoom.DescribeFor(player);
        result.Add(player, new RoomDescriptionEvent(description));

        result.BroadcastToAllButPlayer(exitRoom, player, new PlayerArrivedEvent(player, exitRoom));

        logger.LogDebug("Move complete");

        return Task.FromResult(result);
    }
}

public record RoomDescriptionEvent(string Description) : IGameEvent;

public class RoomDescriptionEventFormatter : IGameEventFormatter<RoomDescriptionEvent>
{
    public string FormatForActor(RoomDescriptionEvent gameEvent) => gameEvent.Description;

    public string FormatForObserver(RoomDescriptionEvent gameEvent) => gameEvent.Description;
}

public record ExitNotFoundEvent(string ExitName) : IGameEvent;

public class ExitNotFoundEventFormatter : IGameEventFormatter<ExitNotFoundEvent>
{
    public string FormatForActor(ExitNotFoundEvent gameEvent) => "That exit doesn't exist.";

    public string FormatForObserver(ExitNotFoundEvent gameEvent) => "That exit doesn't exist.";
}

public record PlayerMovedEvent(Player Player, Room Destination) : IGameEvent;

public class PlayerMovedEventFormatter : IGameEventFormatter<PlayerMovedEvent>
{
    public string FormatForActor(PlayerMovedEvent gameEvent) => gameEvent.Destination.EnterText;

    public string FormatForObserver(PlayerMovedEvent gameEvent) => $"{gameEvent.Player.Username} arrives.";
}

public record PlayerDepartedEvent(Player Player, Room Origin, string ExitName) : IGameEvent;

public class PlayerDepartedEventFormatter : IGameEventFormatter<PlayerDepartedEvent>
{
    public string FormatForActor(PlayerDepartedEvent gameEvent) => gameEvent.Origin.ExitText;

    public string FormatForObserver(PlayerDepartedEvent gameEvent) => $"{gameEvent.Player.Username} leaves.";
}

public record PlayerArrivedEvent(Player Player, Room Destination) : IGameEvent;

public class PlayerArrivedEventFormatter : IGameEventFormatter<PlayerArrivedEvent>
{
    public string FormatForActor(PlayerArrivedEvent gameEvent) => gameEvent.Destination.EnterText;

    public string FormatForObserver(PlayerArrivedEvent gameEvent) => $"{gameEvent.Player.Username} arrives.";
}