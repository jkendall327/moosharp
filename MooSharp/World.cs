using System.Text.Json;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MooSharp;

public class World(IOptions<AppOptions> options, ILogger<World> logger)
{
    private readonly object _syncRoot = new();

    private readonly Dictionary<string, Player> _players = [];
    private Dictionary<RoomId, Room> _rooms = [];

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var dto = await GetWorldDto(cancellationToken);

        var rooms = CreateRooms(dto);
        CreateObjects(dto, rooms);

        lock (_syncRoot)
        {
            _rooms = rooms;
        }
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

    public IReadOnlyCollection<Room> GetRooms()
    {
        lock (_syncRoot)
        {
            return _rooms.Values.ToList();
        }
    }

    public Room GetDefaultRoom()
    {
        lock (_syncRoot)
        {
            return _rooms.Values.First();
        }
    }

    public Room GetRoom(RoomId id)
    {
        lock (_syncRoot)
        {
            return _rooms[id];
        }
    }

    public bool TryGetRoom(RoomId id, [NotNullWhen(true)] out Room? room)
    {
        lock (_syncRoot)
        {
            var found = _rooms.TryGetValue(id, out var value);
            room = value;
            return found;
        }
    }

    public bool TryGetPlayer(ConnectionId connectionId, [NotNullWhen(true)] out Player? player)
    {
        lock (_syncRoot)
        {
            var found = _players.TryGetValue(connectionId.Value, out var value);
            player = value;
            return found;
        }
    }

    public void AddPlayer(ConnectionId connectionId, Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        lock (_syncRoot)
        {
            _players.Add(connectionId.Value, player);
        }
    }

    public bool RemovePlayer(ConnectionId connectionId)
    {
        lock (_syncRoot)
        {
            return _players.Remove(connectionId.Value);
        }
    }

    public Room? GetPlayerLocation(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        lock (_syncRoot)
        {
            return _rooms
                .Values
                .FirstOrDefault(room => room.PlayersInRoom.Contains(player));
        }
    }

    public void MovePlayer(Player player, Room destination)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(destination);

        lock (_syncRoot)
        {
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
}