using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MooSharp.Infrastructure;

namespace MooSharp;

public class WorldClockService(
    IWorldClock worldClock,
    IOptions<WorldClockOptions> options,
    TimeProvider timeProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(options.Value.TickIntervalSeconds);

        using var timer = new PeriodicTimer(interval, timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await worldClock.TriggerTickAsync(stoppingToken);
        }
    }
}
