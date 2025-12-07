using System.Threading.Channels;
using MooSharp.Messaging;

namespace MooSharp.Web.Game;

public class GameEngineBackgroundService(
    GameInputProcessor inputProcessor,
    World.World world,
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