using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooSharp.Data.Worlds;
using MooSharp.Infrastructure;

namespace MooSharp.World;

public class WorldSnapshotService(
    World world,
    IWorldRepository worldRepository,
    IOptions<AppOptions> appOptions,
    TimeProvider timeProvider,
    ILogger<WorldSnapshotService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(appOptions.Value.WorldSnapshotIntervalMinutes);

        using var timer = new PeriodicTimer(interval, timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await SaveSnapshotAsync(stoppingToken);
        }
    }

    private async Task SaveSnapshotAsync(CancellationToken cancellationToken)
    {
        var rooms = world.CreateSnapshot();

        await worldRepository.SaveRoomsAsync(rooms, cancellationToken: cancellationToken);

        logger.LogInformation("World snapshot saved with {RoomCount} rooms", rooms.Count);
    }
}
