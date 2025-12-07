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
    IGameMessageEmitter emitter,
    ILogger<GameInputProcessor> logger)
{
    public async Task ProcessInputAsync(GameInput input, CancellationToken ct = default)
    {
        if (world.Players.TryGetValue(input.ActorId.ToString(), out var player))
        {
            await ProcessWorldCommand(player, input.Command, ct);
        }
        else
        {
            throw new InvalidOperationException(
                $"Got game input for actor {input.ActorId}, but they were not found in the world.");
        }
    }

    private async Task ProcessWorldCommand(Player player, string command, CancellationToken ct = default)
    {
        var parsed = await parser.ParseAsync(player, command, ct);

        if (parsed is null)
        {
            var unparsedError = new GameMessage(player, new SystemMessageEvent("That command wasn't recognised."));

            _ = emitter.SendGameMessagesAsync([unparsedError], ct);

            return;
        }

        try
        {
            var result = await executor.Handle(parsed, ct);

            _ = emitter.SendGameMessagesAsync(result.Messages, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing world command {Command}", command);

            var unexpected = new GameMessage(player, new SystemMessageEvent("An unexpected error occurred."));

            _ = emitter.SendGameMessagesAsync([unexpected], ct);
        }
    }
}