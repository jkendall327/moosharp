using Microsoft.Extensions.Logging.Abstractions;
using MooSharp.Tests.TestDoubles;
using NSubstitute;

namespace MooSharp.Tests;

public class WorldInitializerTests
{
    [Fact]
    public async Task InitializeAsync_LoadsExistingRooms()
    {
        var existingRoom = CreateRoom("existing");
        var (initializer, store, seeder, world) = await CreateInitializerAsync(existingRoomToPersist: existingRoom);

        await initializer.InitializeAsync();

        Assert.Equal(existingRoom.Id, Assert.Single(world.Rooms).Key);
        seeder.DidNotReceiveWithAnyArgs().GetSeedRooms();
    }

    [Fact]
    public async Task InitializeAsync_SeedsAndPersistsWhenStoreEmpty()
    {
        var seedRoom = CreateRoom("seed");
        var seeder = Substitute.For<IWorldSeeder>();
        seeder.GetSeedRooms().Returns([seedRoom]);

        var (initializer, store, _, world) = await CreateInitializerAsync(seeder: seeder);

        await initializer.InitializeAsync();

        var room = Assert.Single(world.Rooms).Value;
        Assert.Equal(seedRoom.Id, room.Id);
        seeder.Received(1).GetSeedRooms();

        var persisted = await store.LoadRoomsAsync();
        Assert.Equal(seedRoom.Id, Assert.Single(persisted).Id);
    }

    [Fact]
    public async Task InitializeAsync_WithProvidedRoomsPersists()
    {
        var providedRoom = CreateRoom("provided");
        var seeder = Substitute.For<IWorldSeeder>();
        seeder.GetSeedRooms().Returns(Array.Empty<Room>());

        var (initializer, store, _, world) = await CreateInitializerAsync(seeder: seeder);

        await initializer.InitializeAsync([providedRoom]);

        Assert.Equal(providedRoom.Id, Assert.Single(world.Rooms).Key);

        var persisted = await store.LoadRoomsAsync();
        Assert.Equal(providedRoom.Id, Assert.Single(persisted).Id);
        seeder.DidNotReceiveWithAnyArgs().GetSeedRooms();
    }

    private static Room CreateRoom(string slug)
    {
        return new Room
        {
            Id = slug,
            Name = $"{slug} name",
            Description = $"{slug} description",
            LongDescription = $"{slug} long description",
            EnterText = string.Empty,
            ExitText = string.Empty
        };
    }

    private static async Task<(WorldInitializer Initializer, InMemoryWorldStore Store, IWorldSeeder Seeder, World World)> CreateInitializerAsync(
        IWorldSeeder? seeder = null,
        Room? existingRoomToPersist = null)
    {
        var store = new InMemoryWorldStore();

        if (existingRoomToPersist is not null)
        {
            await store.SaveRoomsAsync([existingRoomToPersist]);
        }

        var worldSeeder = seeder ?? Substitute.For<IWorldSeeder>();
        var world = new World(store, NullLogger<World>.Instance);
        var initializer = new WorldInitializer(world, store, worldSeeder, NullLogger<WorldInitializer>.Instance);

        return (initializer, store, worldSeeder, world);
    }
}
