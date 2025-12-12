using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;

namespace MooSharp.Commands.Commands.Social;

public class FollowCommand : CommandBase<FollowCommand>
{
    public required Player Player { get; init; }
    public Player? Target { get; init; }
}

public class FollowCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["follow"];

    public CommandCategory Category => CommandCategory.Social;

    public string Description => "Follow another player to move when they move. Usage: follow <player>|stop.";

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        command = null;

        if (ctx.IsFinished)
        {
            return "Follow who?";
        }

        var token = ctx.Peek();

        if (token is not null && (token.Equals("stop", StringComparison.OrdinalIgnoreCase) ||
                                  token.Equals("none", StringComparison.OrdinalIgnoreCase)))
        {
            ctx.Pop();
            command = new FollowCommand
            {
                Player = ctx.Player,
                Target = null
            };

            return null;
        }

        var targetResult = binder.BindPlayerInRoom(ctx);

        if (!targetResult.IsSuccess)
        {
            return targetResult.ErrorMessage;
        }

        command = new FollowCommand
        {
            Player = ctx.Player,
            Target = targetResult.Value
        };

        return null;
    }
}

public class FollowHandler(World.World world) : IHandler<FollowCommand>
{
    public Task<CommandResult> Handle(FollowCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();

        if (cmd.Target is null)
        {
            cmd.Player.FollowTarget = null;

            result.Add(cmd.Player, new FollowStoppedEvent(cmd.Player));

            return Task.FromResult(result);
        }

        if (cmd.Target == cmd.Player)
        {
            result.Add(cmd.Player, new SystemMessageEvent("You cannot follow yourself."));

            return Task.FromResult(result);
        }

        var room = world.GetLocationOrThrow(cmd.Player);

        cmd.Player.FollowTarget = cmd.Target.Id;

        var followEvent = new FollowStartedEvent(cmd.Player, cmd.Target);

        result.Add(cmd.Player, followEvent);
        result.BroadcastToAllButPlayer(room, cmd.Player, followEvent);

        return Task.FromResult(result);
    }
}

public record FollowStartedEvent(Player Player, Player Target) : IGameEvent;

public class FollowStartedEventFormatter : IGameEventFormatter<FollowStartedEvent>
{
    public string FormatForActor(FollowStartedEvent gameEvent) =>
        $"You start following {gameEvent.Target.Username}.";

    public string FormatForObserver(FollowStartedEvent gameEvent) =>
        $"{gameEvent.Player.Username} starts following {gameEvent.Target.Username}.";
}

public record FollowStoppedEvent(Player Player) : IGameEvent;

public class FollowStoppedEventFormatter : IGameEventFormatter<FollowStoppedEvent>
{
    public string FormatForActor(FollowStoppedEvent gameEvent) => "You stop following anyone.";

    public string FormatForObserver(FollowStoppedEvent gameEvent) =>
        $"{gameEvent.Player.Username} stops following anyone.";
}
