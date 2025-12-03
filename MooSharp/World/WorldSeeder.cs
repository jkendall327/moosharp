using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooSharp.Actors;
using MooSharp.Infrastructure;
using MooSharp.World.Dtos;
using Object = MooSharp.Actors.Object;

namespace MooSharp.World;

public interface IWorldSeeder
{
    IReadOnlyCollection<Room> GetSeedRooms();
}

public class WorldSeeder(IOptions<AppOptions> options, ILogger<WorldSeeder> logger) : IWorldSeeder
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        Converters = { new JsonStringEnumConverter<ObjectFlags>() }
    };

    public IReadOnlyCollection<Room> GetSeedRooms()
    {
        var dto = GetWorldDto();

        var rooms = CreateRooms(dto);
        CreateObjects(dto, rooms);

        return rooms.Values.ToList();
    }

    private WorldDto GetWorldDto()
    {
        var path = options.Value.WorldDataFilepath;

        if (string.IsNullOrEmpty(path))
        {
            throw new InvalidOperationException("WorldDataFilepath is not set.");
        }

        var raw = File.ReadAllText(path);

        var dto = JsonSerializer.Deserialize<WorldDto>(raw, JsonSerializerOptions);

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
                CreatorUsername = r.CreatorUsername
            });

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
            if (!rooms.TryGetValue(objectDto.RoomSlug!.Value, out var room))
            {
                continue;
            }

            var obj = new Object
            {
                Description = objectDto.Description,
                Name = objectDto.Name,
                Flags = objectDto.Flags,
                KeyId = objectDto.KeyId,
                CreatorUsername = objectDto.CreatorUsername
            };

            if (!string.IsNullOrWhiteSpace(objectDto.TextContent))
            {
                obj.WriteText(objectDto.TextContent);
            }

            obj.MoveTo(room);
        }
    }
}
