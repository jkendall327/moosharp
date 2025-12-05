using Microsoft.Extensions.Logging;
using MooSharp.Actors;
using MooSharp.Data;
using MooSharp.Data.Mapping;

namespace MooSharp.World;

public class WorldInitializer(World world, IWorldStore worldStore, IWorldSeeder worldSeeder,
    ILogger<WorldInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (await worldStore.HasRoomsAsync(cancellationToken))
        {
            var rooms = await worldStore.LoadRoomsAsync(cancellationToken);
            world.Initialize(WorldSnapshotFactory.CreateRooms(rooms));

            logger.LogInformation("World loaded with {RoomCount} rooms from persistent storage", rooms.Count);
            return;
        }

        var seedRooms = worldSeeder.GetSeedRooms().ToList();

        await worldStore.SaveRoomsAsync(WorldSnapshotFactory.CreateSnapshots(seedRooms), cancellationToken);
        world.Initialize(seedRooms);

        logger.LogInformation("World seeded with {RoomCount} rooms from configuration", seedRooms.Count);
    }

    public async Task InitializeAsync(IEnumerable<Room> rooms, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rooms);

        var roomList = rooms.ToList();

        await worldStore.SaveRoomsAsync(WorldSnapshotFactory.CreateSnapshots(roomList), cancellationToken);
        world.Initialize(roomList);

        logger.LogInformation("World initialized with {RoomCount} provided rooms", roomList.Count);
    }
}
