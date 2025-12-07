using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;
using MooSharp.Features.Chats;

namespace MooSharp.Commands.Commands.Meta;

public class ChannelCommand : CommandBase<ChannelCommand>
{
    public required Player Player { get; init; }
    public required string Channel { get; init; }
    public required string Message { get; init; }
}

public class GlobalChannelCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["/global", "/shout", "/g"];
    public CommandCategory Category => CommandCategory.Meta;
    public string Description => "Send a message to the global channel. Usage: /g <message>.";

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        command = null;
        var message = ctx.GetRemainingText();

        if (string.IsNullOrWhiteSpace(message))
        {
            return "Say what?";
        }

        command = new ChannelCommand
        {
            Player = ctx.Player,
            Channel = ChatChannels.Global,
            Message = message
        };

        return null;
    }
}

public class ChannelCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["channel", "ch", "/channel", "/ch"];
    public CommandCategory Category => CommandCategory.Meta;
    public string Description => "Send a message to a chat channel. Usage: channel <channel> <message> (defaults to global).";

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        command = null;

        // Logic: Peek at the first word. If it is a known channel, consume it.
        // Otherwise, assume the user is typing a message to Global.
        var channel = ChatChannels.Global;
        var nextToken = ctx.Peek();

        if (nextToken != null && ChatChannels.IsValid(nextToken))
        {
            channel = ChatChannels.Normalize(ctx.Pop()!);
        }

        var message = ctx.GetRemainingText();

        if (string.IsNullOrWhiteSpace(message))
        {
            return "Say what?";
        }

        command = new ChannelCommand
        {
            Player = ctx.Player,
            Channel = channel,
            Message = message
        };

        return null;
    }
}

public class ChannelHandler(World.World world) : IHandler<ChannelCommand>
{
    public Task<CommandResult> Handle(ChannelCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();

        // Data is already validated and normalized by Definition/Binder
        var gameEvent = new ChannelMessageEvent(cmd.Player, cmd.Channel, cmd.Message);

        result.Add(cmd.Player, gameEvent);

        var otherPlayers = world.GetActivePlayers()
            .Where(p => p != cmd.Player)
            .Where(p => !p.IsChannelMuted(cmd.Channel));

        result.Broadcast(otherPlayers, gameEvent);

        return Task.FromResult(result);
    }
}

public record ChannelMessageEvent(Player Sender, string Channel, string Message) : IGameEvent;

public class ChannelMessageEventFormatter : IGameEventFormatter<ChannelMessageEvent>
{
    public string FormatForActor(ChannelMessageEvent gameEvent) => Format(gameEvent);
    public string FormatForObserver(ChannelMessageEvent gameEvent) => Format(gameEvent);

    private static string Format(ChannelMessageEvent gameEvent)
        => $"[{gameEvent.Channel}] {gameEvent.Sender.Username}: \"{gameEvent.Message}\"";
}