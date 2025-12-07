using MooSharp.Actors;
using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;
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
    public IReadOnlyCollection<string> Verbs { get; } = ["/global", "/shout"];
    public CommandCategory Category => CommandCategory.Meta;

    public string Description => "Send a message to the global channel. Usage: /g <message>.";

    public ICommand Create(Player player, string args)
        => new ChannelCommand
        {
            Player = player,
            Channel = ChatChannels.Global,
            Message = args
        };
}

public class ChannelCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["channel", "ch", "/channel", "/ch"];
    public CommandCategory Category => CommandCategory.Meta;

    public string Description =>
        "Send a message to a chat channel. Usage: channel <channel> <message> (defaults to global).";

    public ICommand Create(Player player, string args)
    {
        var trimmed = args.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return new ChannelCommand
            {
                Player = player,
                Channel = ChatChannels.Global,
                Message = string.Empty
            };
        }

        var split = trimmed.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (split.Length == 1)
        {
            return new ChannelCommand
            {
                Player = player,
                Channel = ChatChannels.Global,
                Message = trimmed
            };
        }

        var potentialChannel = split[0];

        var channel = ChatChannels.IsValid(potentialChannel)
            ? potentialChannel
            : ChatChannels.Global;

        var message = channel == ChatChannels.Global
            ? trimmed
            : split[1];

        return new ChannelCommand
        {
            Player = player,
            Channel = channel,
            Message = message
        };
    }
}

public class ChannelHandler(World.World world) : IHandler<ChannelCommand>
{
    public Task<CommandResult> Handle(ChannelCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();

        var content = cmd.Message.Trim();
        var channel = ChatChannels.Normalize(cmd.Channel);

        if (string.IsNullOrWhiteSpace(content))
        {
            result.Add(cmd.Player, new SystemMessageEvent("Say what?"));

            return Task.FromResult(result);
        }

        if (!ChatChannels.IsValid(channel))
        {
            result.Add(cmd.Player, new SystemMessageEvent("That channel does not exist."));

            return Task.FromResult(result);
        }

        var gameEvent = new ChannelMessageEvent(cmd.Player, channel, content);

        result.Add(cmd.Player, gameEvent);

        var otherPlayers = world.GetActivePlayers()
            .Where(p => p != cmd.Player)
            .Where(p => !p.IsChannelMuted(channel));

        result.Broadcast(otherPlayers, gameEvent);

        return Task.FromResult(result);
    }
}

public record ChannelMessageEvent(Player Sender, string Channel, string Message) : IGameEvent;

public class ChannelMessageEventFormatter : IGameEventFormatter<ChannelMessageEvent>
{
    public string FormatForActor(ChannelMessageEvent gameEvent) =>
        Format(gameEvent);

    public string FormatForObserver(ChannelMessageEvent gameEvent) =>
        Format(gameEvent);

    private static string Format(ChannelMessageEvent gameEvent)
        => $"[{gameEvent.Channel}] {gameEvent.Sender.Username}: \"{gameEvent.Message}\"";
}
