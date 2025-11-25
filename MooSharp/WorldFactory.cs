using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooSharp.Persistence;

namespace MooSharp;

public class WorldFactory(IOptions<AppOptions> options, ILogger<WorldFactory> logger, IWorldStore worldStore,
    ILogger<World> worldLogger)
{
    public async Task<World> CreateWorldAsync(CancellationToken cancellationToken = default)
    {
        var databaseRooms = await worldStore.LoadRoomsAsync(cancellationToken);

        if (databaseRooms.Any())
        {
            var world = new World(worldStore, worldLogger);
            world.Initialize(databaseRooms);

            return world;
        }

        var dto = GetWorldDto();

        var rooms = CreateRooms(dto);
        CreateObjects(dto, rooms);

        await worldStore.SaveRoomsAsync(rooms.Values, cancellationToken);

        var seededWorld = new World(worldStore, worldLogger);
        seededWorld.Initialize(rooms.Values);

        return seededWorld;
    }

    public async Task<World> CreateWorldAsync(List<Room> rooms, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rooms);

        await worldStore.SaveRoomsAsync(rooms, cancellationToken);

        var world = new World(worldStore, worldLogger);
        world.Initialize(rooms);

        return world;
    }

    private WorldDto GetWorldDto()
    {
        var path = options.Value.WorldDataFilepath;

        if (string.IsNullOrEmpty(path))
        {
            throw new InvalidOperationException("WorldDataFilepath is not set.");
        }

        var raw = File.ReadAllText(path);

        var dto = JsonSerializer.Deserialize<WorldDto>(raw);

        if (dto is null)
        {
            throw new InvalidOperationException("World data failed to deserialize.");
        }

        if (!dto.Rooms.Any())
        {
            throw new InvalidOperationException("Room data failed to deserialize.");
        }

        var slugs = dto
            .Rooms
            .Select(s => s.Slug)
            .ToList();

        var filtered = slugs
            .Distinct()
            .ToList();

        if (slugs.Count != filtered.Count)
        {
            throw new InvalidOperationException("At least one slug was duplicated across rooms.");
        }

        return dto;
    }

    private Dictionary<RoomId, Room> CreateRooms(WorldDto dto)
    {
        var roomActorsBySlug = dto.Rooms.ToDictionary(r => r.Slug,
            r => new Room
            {
                Id = r.Slug,
                Name = r.Name,
                Description = r.Description,
                LongDescription = r.LongDescription,
                EnterText = r.EnterText,
                ExitText = r.ExitText,
            });

        // Connect exits.
        foreach (var roomDto in dto.Rooms)
        {
            try
            {
                logger.LogInformation("Initializing exits for {Room}", roomDto.Slug);

                var currentRoomActor = roomActorsBySlug[roomDto.Slug];

                var exits = roomDto.ConnectedRooms.ToDictionary(exitSlug => exitSlug,
                    exitSlug =>
                    {
                        if (!roomActorsBySlug.ContainsKey(exitSlug))
                        {
                            logger.LogError("Missing slug {Slug} for room {Room}", exitSlug, roomDto.Slug);
                        }

                        return roomActorsBySlug[exitSlug];
                    });

                foreach (var exit in exits)
                {
                    currentRoomActor.Exits.Add(exit.Key, exit.Value.Id);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize exits for {Room}", roomDto.Slug);

                throw;
            }
        }

        return roomActorsBySlug;
    }

    private void CreateObjects(WorldDto dto, Dictionary<RoomId, Room> rooms)
    {
        foreach (var objectDto in dto.Objects.Where(s => s.RoomSlug is not null))
        {
            if (!rooms.TryGetValue(objectDto.RoomSlug!, out var room))
            {
                continue;
            }

            var obj = new Object
            {
                Description = objectDto.Description,
                Name = objectDto.Name
            };

            obj.MoveTo(room);
        }
    }
}
