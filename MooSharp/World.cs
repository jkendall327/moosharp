using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MooSharp;

public class World(IOptions<AppOptions> options, ILogger<World> logger)
{
    public Dictionary<string, Player> Players { get; } = [];
    public Dictionary<RoomId, Room> Rooms { get; private set; } = [];

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var dto = await GetWorldDto(cancellationToken);

        Rooms = CreateRooms(dto);
        CreateObjects(dto, Rooms);
    }

    private async Task<WorldDto> GetWorldDto(CancellationToken cancellationToken)
    {
        var path = options.Value.WorldDataFilepath;

        if (string.IsNullOrEmpty(path))
        {
            throw new InvalidOperationException("WorldDataFilepath is not set.");
        }

        var raw = await File.ReadAllTextAsync(path, cancellationToken);

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
        var bySlug = dto
            .Objects
            .Where(s => s.RoomSlug is not null)
            .ToLookup(r => r.RoomSlug!,
                o => new Object
                {
                    Description = o.Description,
                    Name = o.Name,
                    Location = null,
                    Owner = null
                });

        var dictionary = bySlug.ToDictionary(s => s.Key,
            grouping => grouping
                .Select(o => o)
                .ToList());
        
        foreach (var grouping in bySlug)
        {
            var room = rooms.GetValueOrDefault(grouping.Key);

            if (room is null)
            {
                continue;
            }

            var objectStates = grouping.ToList();
            var objectActors = dictionary[grouping.Key];

            var contents = new Dictionary<string, Object>();

            for (var i = 0; i < objectStates.Count; i++)
            {
                objectStates[i].Location = room;
                contents.Add(objectStates[i].Name, objectActors[i]);
            }

            foreach ((var name, var actor) in contents)
            {
                room.Contents.Add(actor);
            }
        }
    }
}