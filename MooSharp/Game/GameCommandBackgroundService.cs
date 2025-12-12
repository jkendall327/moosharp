using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooSharp.Features.WorldClock;

namespace MooSharp.Game;

public class GameCommandBackgroundService(
    GameInputProcessor inputProcessor,
    World.World world,
    IWorldClock worldClock,
    ChannelReader<GameCommand> reader,
    ILogger<GameCommandBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Game command background service started");

        await foreach (var input in reader.ReadAllAsync(stoppingToken))
        {
            using var scope = logger.BeginScope(new Dictionary<string, object?>
            {
                { "GameCommandType", input.GetType().Name }
            });

            try
            {
                switch (input)
                {
                    case InputCommand ic:
                        await inputProcessor.ProcessInputAsync(ic, stoppingToken);

                        break;

                    case SpawnTreasureCommand stc:
                        logger.LogDebug("Spawning treasure {TreasureType}", stc.Treasure.GetType().Name);
                        world.SpawnTreasureInEmptyRoom([stc.Treasure]);

                        break;

                    case IncrementWorldClockCommand:
                        logger.LogDebug("Triggering world clock tick");
                        await worldClock.TriggerTickAsync(stoppingToken);

                        break;
                }

                // Signal success
                input.CompletionSource?.TrySetResult();
            }
            catch (Exception ex)
            {
                input.CompletionSource?.TrySetException(ex);
                logger.LogError(ex, "Error processing game command");
            }
        }

        logger.LogInformation("Game command background service stopped");
    }
}