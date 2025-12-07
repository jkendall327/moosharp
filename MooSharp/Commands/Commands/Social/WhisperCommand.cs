using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;

namespace MooSharp.Commands.Commands.Social;

public class WhisperCommand : CommandBase<WhisperCommand>
{
    public required Player Player { get; init; }
    // Refactor: We now pass the bound Player object, not the string name
    public required Player Recipient { get; init; } 
    public required string Message { get; init; }
}

public class WhisperCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["whisper"];
    public CommandCategory Category => CommandCategory.Social;
    public string Description => "Send a private message to another player. Usage: whisper <target> <message>.";

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        command = null;

        // 1. Bind the target player (Global search)
        var playerBind = binder.BindOnlinePlayer(ctx);
        if (!playerBind.IsSuccess)
        {
            return playerBind.ErrorMessage;
        }

        // 2. Consume the rest of the string as the message
        var message = ctx.GetRemainingText();
        if (string.IsNullOrWhiteSpace(message))
        {
            return "What do you want to whisper?";
        }

        command = new WhisperCommand
        {
            Player = ctx.Player,
            Recipient = playerBind.Value!,
            Message = message
        };

        return null;
    }
}

public class WhisperHandler : IHandler<WhisperCommand>
{
    public Task<CommandResult> Handle(WhisperCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        
        var whisperEvent = new WhisperEvent(cmd.Player, cmd.Recipient, cmd.Message);

        result.Add(cmd.Player, whisperEvent);
        result.Add(cmd.Recipient, whisperEvent);

        return Task.FromResult(result);
    }
}

public record WhisperEvent(Player Sender, Player Recipient, string Message) : IGameEvent;

public class WhisperEventFormatter : IGameEventFormatter<WhisperEvent>
{
    public string FormatForActor(WhisperEvent gameEvent) =>
        $"You whisper to {gameEvent.Recipient.Username}, \"{gameEvent.Message}\"";

    public string FormatForObserver(WhisperEvent gameEvent) =>
        $"{gameEvent.Sender.Username} whispers to you, \"{gameEvent.Message}\"";
}