using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;

namespace MooSharp.Commands.Commands.Social;

public class EmoteCommand : CommandBase<EmoteCommand>
{
    public required Player Player { get; init; }
    public required string Message { get; init; }
}

public class EmoteCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["/me"];
    public CommandCategory Category => CommandCategory.Social;
    public string Description => "Emote an action to everyone in your current room. Usage: /me <action>.";

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        command = null;

        // "GetRemainingText" is a helper on ParsingContext that joins the remaining tokens
        var message = ctx.GetRemainingText();

        if (string.IsNullOrWhiteSpace(message))
        {
            return "Emote what?";
        }

        command = new EmoteCommand
        {
            Player = ctx.Player,
            Message = message
        };

        return null;
    }
}

public class EmoteHandler(World.World world) : IHandler<EmoteCommand>
{
    public Task<CommandResult> Handle(EmoteCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        
        var room = world.GetLocationOrThrow(cmd.Player);

        var gameEvent = new PlayerEmotedEvent(cmd.Player, cmd.Message);

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