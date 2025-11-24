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

    public Room? GetPlayerLocation(Player player)
    {
        return Rooms
            .Values
            .FirstOrDefault(room => room.PlayersInRoom.Contains(player));
    }

    public void MovePlayer(Player player, Room destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        var origin = GetPlayerLocation(player);

        if (origin == destination)
        {
            return;
        }

        origin?.PlayersInRoom.Remove(player);

        if (!destination.PlayersInRoom.Contains(player))
        {
            destination.PlayersInRoom.Add(player);
        }
    }
}