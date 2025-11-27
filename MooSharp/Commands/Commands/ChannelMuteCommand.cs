using MooSharp.Messaging;

namespace MooSharp;

public class ChannelMuteCommand : CommandBase<ChannelMuteCommand>
{
    public required Player Player { get; init; }
    public required string Channel { get; init; }
    public required bool Mute { get; init; }
}

public class MuteChannelCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["mute", "/mute"];

    public string Description => "Mute a chat channel. Usage: mute <channel>.";

    public ICommand Create(Player player, string args)
        => ChannelMuteCommandDefinitionHelper.Create(player, args, mute: true);
}

public class UnmuteChannelCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["unmute", "/unmute"];

    public string Description => "Unmute a chat channel. Usage: unmute <channel>.";

    public ICommand Create(Player player, string args)
        => ChannelMuteCommandDefinitionHelper.Create(player, args, mute: false);
}

internal static class ChannelMuteCommandDefinitionHelper
{
    public static ICommand Create(Player player, string args, bool mute)
    {
        var channel = args
            .Trim()
            .Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? string.Empty;

        return new ChannelMuteCommand
        {
            Player = player,
            Channel = channel,
            Mute = mute
        };
    }
}

public class ChannelMuteHandler : IHandler<ChannelMuteCommand>
{
    public Task<CommandResult> Handle(ChannelMuteCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();

        var channel = ChatChannels.Normalize(cmd.Channel);

        if (!ChatChannels.IsValid(channel))
        {
            result.Add(cmd.Player, new SystemMessageEvent("That channel does not exist."));

            return Task.FromResult(result);
        }

        var changed = cmd.Mute
            ? cmd.Player.MuteChannel(channel)
            : cmd.Player.UnmuteChannel(channel);

        var message = changed
            ? new SystemMessageEvent(cmd.Mute ? $"Muted {channel}." : $"Unmuted {channel}.")
            : new SystemMessageEvent(cmd.Mute ? $"{channel} is already muted." : $"{channel} is not muted.");

        result.Add(cmd.Player, message);

        return Task.FromResult(result);
    }
}
