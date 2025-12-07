using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MooSharp.Game;

namespace MooSharp.Features.WorldClock;

public class WorldClockService(
    ChannelWriter<GameCommand> writer,
    IOptions<WorldClockOptions> options,
    TimeProvider timeProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(options.Value.TickIntervalSeconds);

        using var timer = new PeriodicTimer(interval, timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await writer.WriteAsync(new IncrementWorldClockCommand(), stoppingToken);
        }
    }
}
