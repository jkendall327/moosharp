using System.Text.Json;
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

public class World(IOptions<AppOptions> appOptions)
{
    public Dictionary<string, RoomActor> Rooms { get; private set; } = [];
    public Dictionary<string, List<ObjectActor>> Objects { get; set; } = [];

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

        var slugs = dto.Rooms
                       .Select(s => s.Slug)
                       .ToList();

        var filtered = slugs.Distinct()
                            .ToList();

        if (slugs.Count != filtered.Count)
        {
            throw new InvalidOperationException("At least one slug was duplicated across rooms.");
        }

        return dto;
    }

    private static async Task<Dictionary<string, RoomActor>> CreateRooms(WorldDto dto)
    {
        var roomActorsBySlug = dto.Rooms.ToDictionary(r => r.Slug,
            r => new RoomActor(new()
            {
                Id = Random.Shared.Next(),
                Slug = r.Slug,
                Name = r.Name,
                Description = r.Description,
            }));

        // Connect exits.
        foreach (var roomDto in dto.Rooms)
        {
            var currentRoomActor = roomActorsBySlug[roomDto.Slug];

            var exits = roomDto.ConnectedRooms.ToDictionary(exitSlug => exitSlug,
                exitSlug => roomActorsBySlug[exitSlug]);

            await currentRoomActor.Ask(new RequestMessage<Room, bool>(roomState =>
            {
                foreach (var exit in exits)
                {
                    roomState.Exits.Add(exit.Key, exit.Value);
                }

                return Task.FromResult(true);
            }));
        }

        return roomActorsBySlug;
    }

    private async Task<Dictionary<string, List<ObjectActor>>> CreateObjects(WorldDto dto)
    {
        var bySlug = dto.Objects
                        .Where(s => s.RoomSlug is not null)
                        .ToLookup(r => r.RoomSlug!,
                            o => new Object()
                            {
                                Id = Random.Shared.Next(),
                                Description = o.Description,
                                Name = o.Name,
                                Location = null,
                                Owner = null
                            });
        
        foreach (var grouping in bySlug)
        {
            var room = Rooms.GetValueOrDefault(grouping.Key);

            foreach (var o in grouping)
            {
                o.Location = room;
            }
            
            // TODO: update room's contents with the object actors   
        }

        var dictionary = bySlug.ToDictionary(s => s.Key,
            o =>
            {
                return o.Select(t => new ObjectActor(t)).ToList();
            });

        return dictionary;
    }
}