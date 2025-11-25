using Microsoft.Extensions.Logging.Abstractions;
using MooSharp.Tests.TestDoubles;

namespace MooSharp.Tests;

public class WorldInitializerTests
{
    [Fact]
    public async Task InitializeAsync_LoadsExistingRooms()
    {
        var store = new InMemoryWorldStore();
        var existingRoom = CreateRoom("existing");
        await store.SaveRoomsAsync([existingRoom]);

        var seeder = new TestWorldSeeder([CreateRoom("seed")]);
        var world = new World(store, NullLogger<World>.Instance);
        var initializer = new WorldInitializer(world, store, seeder, NullLogger<WorldInitializer>.Instance);

        await initializer.InitializeAsync();

        Assert.Equal(existingRoom.Id, Assert.Single(world.Rooms).Key);
        Assert.False(seeder.WasCalled);
    }

    [Fact]
    public async Task InitializeAsync_SeedsAndPersistsWhenStoreEmpty()
    {
        var store = new InMemoryWorldStore();
        var seedRoom = CreateRoom("seed");
        var seeder = new TestWorldSeeder([seedRoom]);
        var world = new World(store, NullLogger<World>.Instance);
        var initializer = new WorldInitializer(world, store, seeder, NullLogger<WorldInitializer>.Instance);

        await initializer.InitializeAsync();

        var room = Assert.Single(world.Rooms).Value;
        Assert.Equal(seedRoom.Id, room.Id);
        Assert.True(seeder.WasCalled);

        var persisted = await store.LoadRoomsAsync();
        Assert.Equal(seedRoom.Id, Assert.Single(persisted).Id);
    }

    [Fact]
    public async Task InitializeAsync_WithProvidedRoomsPersists()
    {
        var store = new InMemoryWorldStore();
        var providedRoom = CreateRoom("provided");
        var seeder = new TestWorldSeeder(Array.Empty<Room>());
        var world = new World(store, NullLogger<World>.Instance);
        var initializer = new WorldInitializer(world, store, seeder, NullLogger<WorldInitializer>.Instance);

        await initializer.InitializeAsync([providedRoom]);

        Assert.Equal(providedRoom.Id, Assert.Single(world.Rooms).Key);

        var persisted = await store.LoadRoomsAsync();
        Assert.Equal(providedRoom.Id, Assert.Single(persisted).Id);
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

    private sealed class TestWorldSeeder(IReadOnlyCollection<Room> rooms) : IWorldSeeder
    {
        public bool WasCalled { get; private set; }

        public IReadOnlyCollection<Room> GetSeedRooms()
        {
            WasCalled = true;
            return rooms;
        }
    }
}
