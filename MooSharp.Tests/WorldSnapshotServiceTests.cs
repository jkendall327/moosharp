using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using MooSharp.Actors.Rooms;
using MooSharp.Data.Worlds;
using MooSharp.Infrastructure;
using MooSharp.World;
using NSubstitute;

namespace MooSharp.Tests;

public class WorldSnapshotServiceTests
{
    [Fact]
    public async Task SavesWorldSnapshotOnInterval()
    {
        var repository = Substitute.For<IWorldRepository>();
        var room = new Room
        {
            Id = "room-1",
            Name = "Test Room",
            Description = "Description",
            LongDescription = "Long description",
            EnterText = string.Empty,
            ExitText = string.Empty
        };

        var world = new World.World(repository, NullLogger<World.World>.Instance);
        world.Initialize([room]);

        var options = Options.Create(new AppOptions
        {
            DatabaseFilepath = "db.sqlite",
            WorldDataFilepath = "world.json",
            WorldSnapshotIntervalMinutes = 5
        });

        var timeProvider = new FakeTimeProvider();
        var service = new WorldSnapshotService(world, repository, options, timeProvider,
            NullLogger<WorldSnapshotService>.Instance);

        IEnumerable<RoomSnapshotDto>? savedSnapshots = null;
        var snapshotSaved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        repository
            .SaveRoomsAsync(Arg.Any<IEnumerable<RoomSnapshotDto>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                savedSnapshots = ci.ArgAt<IEnumerable<RoomSnapshotDto>>(0);
                snapshotSaved.TrySetResult();

                return Task.CompletedTask;
            });

        using var cts = new CancellationTokenSource();

        await service.StartAsync(cts.Token);

        timeProvider.Advance(TimeSpan.FromMinutes(5));

        await snapshotSaved.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.NotNull(savedSnapshots);
        Assert.Equal(room.Id.Value, Assert.Single(savedSnapshots!).Id);

        cts.Cancel();
        await service.StopAsync(CancellationToken.None);
    }
}
