using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;

namespace MooSharp.Commands.Commands.Social;

public class YellCommand : CommandBase<YellCommand>
{
    public required Player Player { get; init; }
    public required string Message { get; init; }
}

public class YellCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["yell"];
    public CommandCategory Category => CommandCategory.Social;
    public string Description => "Yell a message to everyone in your current room and connected rooms. Usage: yell <message>.";

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        command = null;

        var message = ctx.GetRemainingText();

        if (string.IsNullOrWhiteSpace(message))
        {
            return "Yell what?";
        }

        command = new YellCommand
        {
            Player = ctx.Player,
            Message = message
        };

        return null;
    }
}

public class YellHandler(World.World world) : IHandler<YellCommand>
{
    public Task<CommandResult> Handle(YellCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var room = world.GetLocationOrThrow(cmd.Player);

        // Event for the yeller and people in the same room
        var yelledEvent = new PlayerYelledEvent(cmd.Player, cmd.Message);

        result.Add(cmd.Player, yelledEvent);
        result.BroadcastToAllButPlayer(room, cmd.Player, yelledEvent);

        // Broadcast to connected rooms
        var heardYellEvent = new PlayerHeardYellEvent(cmd.Message);

        foreach (var exit in room.Exits)
        {
            if (world.Rooms.TryGetValue(exit.Destination, out var destinationRoom))
            {
                result.Broadcast(destinationRoom.PlayersInRoom, heardYellEvent);
            }
        }

        return Task.FromResult(result);
    }
}

public record PlayerYelledEvent(Player Player, string Message) : IGameEvent;

public class PlayerYelledEventFormatter : IGameEventFormatter<PlayerYelledEvent>
{
    public string FormatForActor(PlayerYelledEvent gameEvent) => $"You yell, \"{gameEvent.Message}\"";
    public string FormatForObserver(PlayerYelledEvent gameEvent) => $"{gameEvent.Player.Username} yells, \"{gameEvent.Message}\"";
}

public record PlayerHeardYellEvent(string Message) : IGameEvent;

public class PlayerHeardYellEventFormatter : IGameEventFormatter<PlayerHeardYellEvent>
{
    // FormatForActor and FormatForObserver should probably be the same here since the player receiving this is observing the yell from afar
    // The "Actor" concept here usually applies to the person initiating the event, but for this event, the audience are all observers in connected rooms.
    // However, IGameEventFormatter requires both methods.

    public string FormatForActor(PlayerHeardYellEvent gameEvent) => $"Someone yells from nearby, \"{gameEvent.Message}\"";
    public string FormatForObserver(PlayerHeardYellEvent gameEvent) => $"Someone yells from nearby, \"{gameEvent.Message}\"";
}
