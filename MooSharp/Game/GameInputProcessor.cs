using Microsoft.Extensions.Logging;
using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Presentation;
using MooSharp.Infrastructure.Messaging;

namespace MooSharp.Game;

public class GameInputProcessor(
    World.World world,
    CommandParser parser,
    CommandExecutor executor,
    IGameMessageEmitter emitter,
    ILogger<GameInputProcessor> logger)
{
    public async Task ProcessInputAsync(InputCommand inputCommand, CancellationToken ct = default)
    {
        var player = world.TryGetPlayer(inputCommand.ActorId);
        
        if (player is not null)
        {
            await ProcessWorldCommand(player, inputCommand.Command, ct);
        }
        else
        {
            throw new InvalidOperationException(
                $"Got game input for actor {inputCommand.ActorId}, but they were not found in the world.");
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