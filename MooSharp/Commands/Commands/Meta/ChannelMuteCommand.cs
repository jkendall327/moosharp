using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;
using MooSharp.Features.Chats;

namespace MooSharp.Commands.Commands.Meta;

public class ChannelMuteCommand : CommandBase<ChannelMuteCommand>
{
    public required Player Player { get; init; }
    public required string Channel { get; init; }
    public required bool Mute { get; init; }
}

public class MuteChannelCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["mute", "/mute"];
    public CommandCategory Category => CommandCategory.Meta;
    public string Description => "Mute a chat channel. Usage: mute <channel>.";

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
        => ChannelMuteCommandDefinitionHelper.TryCreate(ctx, binder, mute: true, out command);
}

public class UnmuteChannelCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["unmute", "/unmute"];
    public CommandCategory Category => CommandCategory.Meta;
    public string Description => "Unmute a chat channel. Usage: unmute <channel>.";

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
        => ChannelMuteCommandDefinitionHelper.TryCreate(ctx, binder, mute: false, out command);
}

internal static class ChannelMuteCommandDefinitionHelper
{
    public static string? TryCreate(ParsingContext ctx, ArgumentBinder binder, bool mute, out ICommand? command)
    {
        command = null;

        var channelResult = binder.BindChannelName(ctx);
        if (!channelResult.IsSuccess)
        {
            return channelResult.ErrorMessage;
        }

        command = new ChannelMuteCommand
        {
            Player = ctx.Player,
            Channel = channelResult.Value!,
            Mute = mute
        };

        return null;
    }
}

public class ChannelMuteHandler : IHandler<ChannelMuteCommand>
{
    public Task<CommandResult> Handle(ChannelMuteCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();

        // No need to validate ChatChannels.IsValid here anymore.

        var changed = cmd.Mute
            ? cmd.Player.MuteChannel(cmd.Channel)
            : cmd.Player.UnmuteChannel(cmd.Channel);

        var message = changed
            ? new(cmd.Mute ? $"Muted {cmd.Channel}." : $"Unmuted {cmd.Channel}.")
            : new SystemMessageEvent(cmd.Mute ? $"{cmd.Channel} is already muted." : $"{cmd.Channel} is not muted.");

        result.Add(cmd.Player, message);

        return Task.FromResult(result);
    }
}