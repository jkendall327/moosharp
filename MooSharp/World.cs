using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MooSharp;

public class WorldDto
{
    public List<RoomDto> Rooms { get; init; } = [];
    public List<ObjectDto> Objects { get; init; } = [];
}

public class RoomDto
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Slug { get; set; }
    public List<string> ConnectedRooms { get; set; } = [];
}

public class ObjectDto
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public string? RoomSlug { get; set; }
}

public class World(IOptions<AppOptions> appOptions, ILoggerFactory loggerFactory)
{
    public List<Player> Players { get; set; } = [];
    public Dictionary<string, Room> Rooms { get; private set; } = [];
    public Dictionary<string, List<Object>> Objects { get; set; } = [];

    private readonly ILogger<World> _logger = loggerFactory.CreateLogger<World>();

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var dto = await GetWorldDto(cancellationToken);

        Rooms = await CreateRooms(dto);
        Objects = await CreateObjects(dto);
    }

    private async Task<WorldDto> GetWorldDto(CancellationToken cancellationToken)
    {
        var path = appOptions.Value.WorldDataFilepath;

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

    private async Task<Dictionary<string, Room>> CreateRooms(WorldDto dto)
    {
        var roomActorsBySlug = dto.Rooms.ToDictionary(r => r.Slug,
            r => new Room()
                {
                    Id = Random.Shared.Next(),
                    Slug = r.Slug,
                    Name = r.Name,
                    Description = r.Description,
                });
        
        // Connect exits.
        foreach (var roomDto in dto.Rooms)
        {
            try
            {
                _logger.LogInformation("Initializing exits for {Room}", roomDto.Slug);

                var currentRoomActor = roomActorsBySlug[roomDto.Slug];

                var exits = roomDto.ConnectedRooms.ToDictionary(exitSlug => exitSlug,
                    exitSlug =>
                    {
                        if (!roomActorsBySlug.ContainsKey(exitSlug))
                        {
                            _logger.LogError("Missing slug {Slug} for room {Room}", exitSlug, roomDto.Slug);
                        }

                        return roomActorsBySlug[exitSlug];
                    });

                foreach (var exit in exits)
                {
                    currentRoomActor.Exits.Add(exit.Key, exit.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize exits for {Room}", roomDto.Slug);

                throw;
            }
        }

        return roomActorsBySlug;
    }

    private async Task<Dictionary<string, List<Object>>> CreateObjects(WorldDto dto)
    {
        var bySlug = dto
            .Objects
            .Where(s => s.RoomSlug is not null)
            .ToLookup(r => r.RoomSlug!,
                o => new Object
                {
                    Id = Random.Shared.Next(),
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
            var room = Rooms.GetValueOrDefault(grouping.Key);

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
                room.Contents.Add(name, actor);
            }
        }

        return dictionary;
    }
}