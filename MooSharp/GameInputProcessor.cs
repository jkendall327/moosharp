using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooSharp.Actors;
using MooSharp.Agents;
using MooSharp.Commands;
using MooSharp.Commands.Commands;
using MooSharp.Commands.Machinery;
using MooSharp.Data;
using MooSharp.Data.Dtos;
using MooSharp.Data.Mapping;
using MooSharp.Infrastructure;
using MooSharp.Messaging;
using Object = MooSharp.Actors.Object;

namespace MooSharp;

public class GameInputProcessor(
    World.World world,
    CommandParser parser,
    CommandExecutor executor,
    IRawMessageSender sender,
    ILogger<GameInputProcessor> logger)
{
    public async Task ProcessInputAsync(GameInput input, CancellationToken ct = default)
    {
        switch (input.Command)
        {
            case WorldCommand wc:
                if (!world.Players.TryGetValue(input.ConnectionId.Value, out var player))
                {
                    await sender.SendLoginRequiredMessageAsync(input.ConnectionId, ct);

                    break;
                }

                await ProcessWorldCommand(wc, player, ct);

                break;
            default: throw new ArgumentOutOfRangeException(nameof(input.Command));
        }
    }

    private async Task ProcessWorldCommand(WorldCommand command, Player player, CancellationToken ct = default)
    {
        var parsed = await parser.ParseAsync(player, command.Command, ct);

        if (parsed is null)
        {
            _ = sender.SendGameMessagesAsync([
                    new(player, new SystemMessageEvent("That command wasn't recognised."))
                ],
                ct);

            return;
        }

        try
        {
            var result = await executor.Handle(parsed, ct);

            _ = sender.SendGameMessagesAsync(result.Messages, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing world command {Command}", command.Command);

            _ = sender.SendGameMessagesAsync([
                    new(player, new SystemMessageEvent("An unexpected error occurred."))
                ],
                ct);
        }
    }
}