using System.Threading.Channels;
using MooSharp.Messaging;

namespace MooSharp.Web.Game;

public class GameEngineBackgroundService(
    GameInputProcessor inputProcessor,
    ChannelReader<GameInput> reader,
    ILogger<GameEngineBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var input in reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await inputProcessor.ProcessInputAsync(input, stoppingToken);

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