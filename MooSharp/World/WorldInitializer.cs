using Microsoft.Extensions.Logging;
using MooSharp.Actors;
using MooSharp.Actors.Rooms;
using MooSharp.Data;
using MooSharp.Data.Worlds;

namespace MooSharp.World;

public class WorldInitializer(World world, IWorldRepository worldRepository, IWorldSeeder worldSeeder,
    ILogger<WorldInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (await worldRepository.HasRoomsAsync(cancellationToken))
        {
            var rooms = await worldRepository.LoadRoomsAsync(cancellationToken);
            world.Initialize(WorldSnapshotFactory.CreateRooms(rooms));

            logger.LogInformation("World loaded with {RoomCount} rooms from persistent storage", rooms.Count);
            return;
        }

        var seedRooms = worldSeeder.GetSeedRooms().ToList();

        await worldRepository.SaveRoomsAsync(WorldSnapshotFactory.CreateSnapshots(seedRooms), cancellationToken);
        world.Initialize(seedRooms);

        logger.LogInformation("World seeded with {RoomCount} rooms from configuration", seedRooms.Count);
    }

    public async Task InitializeAsync(IEnumerable<Room> rooms, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rooms);

        var roomList = rooms.ToList();

        await worldRepository.SaveRoomsAsync(WorldSnapshotFactory.CreateSnapshots(roomList), cancellationToken);
        world.Initialize(roomList);

        logger.LogInformation("World initialized with {RoomCount} provided rooms", roomList.Count);
    }
}
