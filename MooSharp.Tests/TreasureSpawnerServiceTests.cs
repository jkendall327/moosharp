using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using MooSharp.Infrastructure;
using MooSharp.Persistence;
using NSubstitute;

namespace MooSharp.Tests;

public class TreasureSpawnerServiceTests
{
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly World _world = CreateWorldWithEmptyRooms(roomCount: 3);

    [Fact]
    public async Task ExecuteAsync_SpawnsTreasureAfterIntervalElapsed()
    {
        // Arrange
        var options = Options.Create(new TreasureSpawnerOptions { SpawnIntervalMinutes = 5 });
        var service = new TreasureSpawnerService(_world, options, _timeProvider);

        using var cts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(cts.Token);

        // Advance time past the spawn interval
        _timeProvider.Advance(TimeSpan.FromMinutes(5));

        await cts.CancelAsync();
        await serviceTask;

        // Assert
        var allItems = _world.Rooms.Values
            .SelectMany(r => r.Contents)
            .ToList();

        Assert.Single(allItems);
        Assert.True(allItems[0].Value > 0, "Spawned treasure should have a value greater than 0");
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotSpawnBeforeIntervalElapsed()
    {
        // Arrange
        var options = Options.Create(new TreasureSpawnerOptions { SpawnIntervalMinutes = 5 });
        var service = new TreasureSpawnerService(_world, options, _timeProvider);

        using var cts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(cts.Token);

        // Advance time but not past the interval
        _timeProvider.Advance(TimeSpan.FromMinutes(3));

        await cts.CancelAsync();
        await serviceTask;

        // Assert
        var allItems = _world.Rooms.Values
            .SelectMany(r => r.Contents)
            .ToList();

        Assert.Empty(allItems);
    }

    [Fact]
    public async Task ExecuteAsync_SpawnsMultipleTreasuresOverTime()
    {
        // Arrange
        var world = CreateWorldWithEmptyRooms(roomCount: 5);
        var timeProvider = new FakeTimeProvider();
        var options = Options.Create(new TreasureSpawnerOptions { SpawnIntervalMinutes = 2 });

        var service = new TreasureSpawnerService(world, options, timeProvider);

        using var cts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(cts.Token);

        // Advance through 3 intervals
        for (var i = 0; i < 3; i++)
        {
            timeProvider.Advance(TimeSpan.FromMinutes(2));
            await Task.Delay(50, cts.Token);
        }

        await cts.CancelAsync();
        await serviceTask;

        // Assert
        var allItems = world.Rooms.Values
            .SelectMany(r => r.Contents)
            .ToList();

        Assert.Equal(3, allItems.Count);
        Assert.All(allItems, item => Assert.True(item.Value > 0));
    }

    private static World CreateWorldWithEmptyRooms(int roomCount)
    {
        var world = new World(Substitute.For<IWorldStore>(), NullLogger<World>.Instance);

        var rooms = Enumerable.Range(1, roomCount)
            .Select(i => new Room
            {
                Id = new RoomId($"room-{i}"),
                Name = $"Room {i}",
                Description = $"Test room {i}",
                LongDescription = $"A test room numbered {i}",
                EnterText = "You enter.",
                ExitText = "You leave."
            })
            .ToList();

        world.Initialize(rooms);

        return world;
    }
}
