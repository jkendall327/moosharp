using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;

namespace MooSharp.Commands.Commands;

public class RecallCommand : CommandBase<RecallCommand>
{
    public required Player Player { get; init; }
}

public class RecallCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["recall", "home"];

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        command = new RecallCommand
        {
            Player = ctx.Player,
        };
        
        return null;
    }

    public CommandCategory Category => CommandCategory.General;

    public string Description => "Return to the Atrium. Usage: recall.";
}

public class RecallHandler(World.World world) : IHandler<RecallCommand>
{
    public Task<CommandResult> Handle(RecallCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var player = cmd.Player;
        var destination = world.GetDefaultRoom();

        var recallEvent = new PlayerRecalledEvent(player);
        result.Add(player, recallEvent);

        var origin = world.GetPlayerLocation(player);
        if (origin is not null)
        {
            result.BroadcastToAllButPlayer(origin, player, recallEvent);
        }

        world.MovePlayer(player, destination);

        result.Add(player, new PlayerMovedEvent(player, destination));

        var description = destination.DescribeFor(player);
        result.Add(player, new RoomDescriptionEvent(description));

        result.BroadcastToAllButPlayer(destination, player, new PlayerArrivedEvent(player, destination));

        return Task.FromResult(result);
    }
}

public record PlayerRecalledEvent(Player Player) : IGameEvent;

public class PlayerRecalledEventFormatter : IGameEventFormatter<PlayerRecalledEvent>
{
    public string FormatForActor(PlayerRecalledEvent gameEvent) => "You feel a tug at your navel and vanish...";

    public string FormatForObserver(PlayerRecalledEvent gameEvent) =>
        $"{gameEvent.Player.Username} vanishes in a blink.";
}
