using MooSharp.Messaging;

namespace MooSharp;

public class WhisperCommand : CommandBase<WhisperCommand>
{
    public required Player Player { get; init; }
    public required string Target { get; init; }
    public required string Message { get; init; }
}

public class WhisperCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["whisper"];

    public string Description => "Send a private message to another player. Usage: whisper <target> <message>.";

    public ICommand Create(Player player, string args)
    {
        var split = args.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new WhisperCommand
        {
            Player = player,
            Target = split.ElementAtOrDefault(0) ?? string.Empty,
            Message = split.ElementAtOrDefault(1) ?? string.Empty
        };
    }
}

public class WhisperHandler(World world) : IHandler<WhisperCommand>
{
    public Task<CommandResult> Handle(WhisperCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var targetName = cmd.Target.Trim();
        var message = cmd.Message.Trim();

        if (string.IsNullOrWhiteSpace(targetName) || string.IsNullOrWhiteSpace(message))
        {
            result.Add(cmd.Player, new SystemMessageEvent("Usage: whisper <target> <message>."));
            return Task.FromResult(result);
        }

        var recipient = world.Players
            .Values
            .FirstOrDefault(p => p.Username.Equals(targetName, StringComparison.OrdinalIgnoreCase));

        if (recipient is null)
        {
            result.Add(cmd.Player, new SystemMessageEvent("That player isn't online."));
            return Task.FromResult(result);
        }

        var whisperEvent = new WhisperEvent(cmd.Player, recipient, message);

        result.Add(cmd.Player, whisperEvent);
        result.Add(recipient, whisperEvent);

        return Task.FromResult(result);
    }
}
