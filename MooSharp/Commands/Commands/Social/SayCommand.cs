using MooSharp.Messaging;

namespace MooSharp;

public class SayCommand : CommandBase<SayCommand>
{
    public required Player Player { get; init; }
    public required string Message { get; init; }
}

public class SayCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["say", "s"];

    public string Description => "Send a message to everyone in your current room. Usage: say <message>.";

    public ICommand Create(Player player, string args)
        => new SayCommand
        {
            Player = player,
            Message = args
        };
}

public class SayHandler(World world) : IHandler<SayCommand>
{
    public Task<CommandResult> Handle(SayCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();

        var content = cmd.Message.Trim();

        if (string.IsNullOrWhiteSpace(content))
        {
            result.Add(cmd.Player, new SystemMessageEvent("Say what?"));

            return Task.FromResult(result);
        }

        var room = world.GetPlayerLocation(cmd.Player)
            ?? throw new InvalidOperationException("Player has no known current location.");

        var gameEvent = new PlayerSaidEvent(cmd.Player, content);

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
