using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;

namespace MooSharp.Commands.Commands.Meta;

public class WaitCommand : CommandBase<WaitCommand>
{
    public required Player Player { get; init; }
}

public class WaitCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["wait", "z"];
    public CommandCategory Category => CommandCategory.Meta;
    public string Description => "Pass a turn without acting.";

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        command = new WaitCommand
        {
            Player = ctx.Player
        };

        return null;
    }
}

public class WaitHandler(World.World world) : IHandler<WaitCommand>
{
    public Task<CommandResult> Handle(WaitCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var room = world.GetLocationOrThrow(cmd.Player);
        var gameEvent = new PlayerWaitsEvent(cmd.Player);

        result.Add(cmd.Player, gameEvent);
        result.BroadcastToAllButPlayer(room, cmd.Player, gameEvent);

        return Task.FromResult(result);
    }
}

public record PlayerWaitsEvent(Player Player) : IGameEvent;

public class PlayerWaitsEventFormatter : IGameEventFormatter<PlayerWaitsEvent>
{
    public string FormatForActor(PlayerWaitsEvent gameEvent) => "Time passes...";
    public string FormatForObserver(PlayerWaitsEvent gameEvent) => $"{gameEvent.Player.Username} waits.";
}
