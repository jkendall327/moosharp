using System.Threading.Channels;

namespace MooSharp;

public class GameEngineBackgroundService(
    GameEngine engine,
    ChannelReader<GameInput> reader,
    ILogger<GameEngineBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var input in reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await engine.ProcessInputAsync(input, stoppingToken);

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