using Microsoft.Extensions.Logging.Abstractions;
using MooSharp.Actors;
using MooSharp.Data.Mapping;
using MooSharp.Tests.TestDoubles;

namespace MooSharp.Tests.Handlers;

public static class HandlerTestHelpers
{
    public static Task<World.World> CreateWorld(params Room[] rooms)
    {
        var store = new InMemoryWorldStore();
        var world = new World.World(store, NullLogger<World.World>.Instance);

        world.Initialize(rooms);

        return Task.FromResult(world);
    }

    public static async Task<World.World> CreateWorld(InMemoryWorldStore store, params Room[] rooms)
    {
        var world = new World.World(store, NullLogger<World.World>.Instance);

        world.Initialize(rooms);

        await store.SaveRoomsAsync(WorldSnapshotFactory.CreateSnapshots(rooms));

        return world;
    }

    public static Room CreateRoom(string slug, string? creator = null)
    {
        return new()
        {
            Id = slug,
            Name = $"{slug} name",
            Description = $"{slug} description",
            LongDescription = $"{slug} long description",
            EnterText = "",
            ExitText = "",
            CreatorUsername = creator
        };
    }

    public static Player CreatePlayer(string? username = null)
    {
        return new()
        {
            Username = username ?? "Player",
            Connection = new TestPlayerConnection()
        };
    }
}