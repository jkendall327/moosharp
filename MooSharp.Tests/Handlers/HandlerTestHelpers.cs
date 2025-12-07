using Microsoft.Extensions.Logging.Abstractions;
using MooSharp.Actors.Players;
using MooSharp.Actors.Rooms;
using MooSharp.Tests.TestDoubles;
using MooSharp.World;

namespace MooSharp.Tests.Handlers;

public static class HandlerTestHelpers
{
    public static Task<World.World> CreateWorld(params Room[] rooms)
    {
        var store = new InMemoryWorldRepository();
        var world = new World.World(store, NullLogger<World.World>.Instance);

        world.Initialize(rooms);

        return Task.FromResult(world);
    }

    public static async Task<World.World> CreateWorld(InMemoryWorldRepository repository, params Room[] rooms)
    {
        var world = new World.World(repository, NullLogger<World.World>.Instance);

        world.Initialize(rooms);

        await repository.SaveRoomsAsync(WorldSnapshotFactory.CreateSnapshots(rooms));

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
            Id = PlayerId.New(),
            Username = username ?? "Player"
        };
    }
}