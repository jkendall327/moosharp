using System.Collections.Concurrent;
using MooSharp.Persistence;
using Microsoft.Extensions.Logging;

namespace MooSharp;

public class World(IWorldStore worldStore, ILogger<World> logger)
{
    public ConcurrentDictionary<string, Player> Players { get; } = new();
    public IReadOnlyDictionary<RoomId, Room> Rooms => _rooms;
    private readonly Dictionary<RoomId, Room> _rooms = new();

    private readonly Dictionary<Player, Room> _playerLocations = [];

    public void Initialize(IEnumerable<Room> rooms)
    {
        ArgumentNullException.ThrowIfNull(rooms);

        _rooms.Clear();
        _playerLocations.Clear();

        foreach (var room in rooms)
        {
            _rooms[room.Id] = room;
        }
    }

    public Room? GetPlayerLocation(Player player)
    {
        _ = _playerLocations.TryGetValue(player, out var room);

        return room;
    }

    public void MovePlayer(Player player, Room destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        var origin = GetPlayerLocation(player);

        if (origin == destination)
        {
            return;
        }

        origin?.RemovePlayer(player);

        destination.AddPlayer(player);

        _playerLocations[player] = destination;
    }

    public void RemovePlayer(Player player)
    {
        if (_playerLocations.Remove(player, out var location))
        {
            location.RemovePlayer(player);
        }
    }

    public async Task<Room> CreateRoomAsync(string slug, string name, string description, string longDescription,
        string enterText, string exitText, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        var room = new Room
        {
            Id = new RoomId(slug),
            Name = name,
            Description = description,
            LongDescription = longDescription,
            EnterText = enterText,
            ExitText = exitText
        };

        _rooms[room.Id] = room;

        await worldStore.SaveRoomAsync(room, cancellationToken);

        logger.LogInformation("Room {RoomName} ({RoomId}) created", room.Name, room.Id);

        return room;
    }

    public async Task AddExitAsync(Room origin, Room destination, string direction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentException.ThrowIfNullOrWhiteSpace(direction);

        origin.Exits[direction] = destination.Id;

        await worldStore.SaveExitAsync(origin.Id, destination.Id, direction, cancellationToken);
    }

    public async Task UpdateRoomDescriptionAsync(Room room, string description,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(room);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        room.Description = description;
        room.LongDescription = description;

        await worldStore.UpdateRoomDescriptionAsync(room.Id, description, description, cancellationToken);

        logger.LogInformation("Room {RoomName} ({RoomId}) description updated", room.Name, room.Id);
    }
}
