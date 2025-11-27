using MooSharp.Messaging;

namespace MooSharp;

public class EmoteCommand : CommandBase<EmoteCommand>
{
    public required Player Player { get; init; }
    public required string Message { get; init; }
}

public class EmoteCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["/me"];

    public string Description => "Emote an action to everyone in your current room. Usage: /me <action>.";

    public ICommand Create(Player player, string args) =>
        new EmoteCommand
        {
            Player = player,
            Message = args
        };
}

public class EmoteHandler(World world) : IHandler<EmoteCommand>
{
    public Task<CommandResult> Handle(EmoteCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var content = cmd.Message.Trim();

        if (string.IsNullOrWhiteSpace(content))
        {
            result.Add(cmd.Player, new SystemMessageEvent("Emote what?"));
            return Task.FromResult(result);
        }

        var room = world.GetPlayerLocation(cmd.Player)
            ?? throw new InvalidOperationException("Player has no known current location.");

        var gameEvent = new PlayerEmotedEvent(cmd.Player, content);

        result.Broadcast(room.PlayersInRoom, gameEvent);

        return Task.FromResult(result);
    }
}

public record PlayerEmotedEvent(Player Player, string Message) : IGameEvent;

public class PlayerEmotedEventFormatter : IGameEventFormatter<PlayerEmotedEvent>
{
    public string FormatForActor(PlayerEmotedEvent gameEvent) =>
        $"{gameEvent.Player.Username} {gameEvent.Message}";

    public string FormatForObserver(PlayerEmotedEvent gameEvent) =>
        $"{gameEvent.Player.Username} {gameEvent.Message}";
}
