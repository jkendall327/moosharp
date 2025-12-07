using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;

namespace MooSharp.Commands.Commands.Social;

public class SayCommand : CommandBase<SayCommand>
{
    public required Player Player { get; init; }
    public required string Message { get; init; }
}

public class SayCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["say", "s"];
    public CommandCategory Category => CommandCategory.Social;
    public string Description => "Send a message to everyone in your current room. Usage: say <message>.";

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        command = null;

        var message = ctx.GetRemainingText();

        if (string.IsNullOrWhiteSpace(message))
        {
            return "Say what?";
        }

        command = new SayCommand
        {
            Player = ctx.Player,
            Message = message
        };

        return null;
    }
}

public class SayHandler(World.World world) : IHandler<SayCommand>
{
    public Task<CommandResult> Handle(SayCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var room = world.GetLocationOrThrow(cmd.Player);
        var gameEvent = new PlayerSaidEvent(cmd.Player, cmd.Message);

        result.Add(cmd.Player, gameEvent);
        result.BroadcastToAllButPlayer(room, cmd.Player, gameEvent);

        return Task.FromResult(result);
    }
}

public record PlayerSaidEvent(Player Player, string Message) : IGameEvent;

public class PlayerSaidEventFormatter : IGameEventFormatter<PlayerSaidEvent>
{
    public string FormatForActor(PlayerSaidEvent gameEvent) => $"[{gameEvent.Player.Username}]: \"{gameEvent.Message}\"";
    public string FormatForObserver(PlayerSaidEvent gameEvent) => FormatForActor(gameEvent);
}