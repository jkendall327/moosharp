using System.Text.Json;
using Microsoft.Extensions.Options;

namespace MooSharp;

public class WorldDto
{
    public List<RoomDto> Rooms { get; set; } = [];
    public List<ObjectDto> Objects { get; set; } = [];
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
    public List<RoomActor> Rooms { get; } = new();

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
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

        var rooms = dto.Rooms
                       .Select((r, i) => new Room
                       {
                           Id = i,
                           Name = r.Name,
                           Description = r.Description,
                           Contents =
                           {
                           },
                           Exits =
                           {
                           }
                       })
                       .ToList();

        var objects = dto.Objects.Select((o, i) => new Object
        {
            Id = i,
            Description = o.Description,
            Name = o.Name,
            Location = null,
            Owner = null
        });

        var room = new Room
        {
            Id = 1,
            Name = "Atrium",
            Description = "A beautiful antechamber",
        };

        var atrium = new RoomActor(room);

        var sideroom = new RoomActor(new()
        {
            Id = 2,
            Name = "Side-room",
            Description = "A small but clean break-room for drinking coffee",
            Exits =
            {
                {
                    "atrium", atrium
                }
            }
        });

        room.Exits.Add("side-room", sideroom);

        Rooms.Add(atrium);
        Rooms.Add(sideroom);

        room.Contents.Add("Cup",
            new(new()
            {
                Id = 4,
                Name = "Cup",
                Description = "It's a small, finely-wrought coffee cup.",
                Location = sideroom
            }));
    }
}