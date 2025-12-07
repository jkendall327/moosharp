using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooSharp.Messaging;
using MooSharp.World;

namespace MooSharp.Web.Game;

public class GameEngineBackgroundService(
    GameInputProcessor inputProcessor,
    World.World world,
    IWorldClock worldClock,
    ChannelReader<GameCommand> reader,
    ILogger<GameEngineBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var input in reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                switch (input)
                {
                    case InputCommand ic:
                        await inputProcessor.ProcessInputAsync(ic, stoppingToken);

                        break;

                    case SpawnTreasureCommand stc:
                        world.SpawnTreasureInEmptyRoom([stc.Treasure]);

                        break;

                    case IncrementWorldClockCommand:
                        await worldClock.TriggerTickAsync(stoppingToken);

                        break;
                }

                // Signal success
                input.CompletionSource?.TrySetResult();
            }
            catch (Exception ex)
            {
                input.CompletionSource?.TrySetException(ex);
                logger.LogError(ex, "Error processing input");
            }
        }
    }
}