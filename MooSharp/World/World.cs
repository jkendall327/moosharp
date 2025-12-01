using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using MooSharp.Actors;
using MooSharp.Persistence;
using Object = MooSharp.Actors.Object;

namespace MooSharp.World;

public class World(IWorldStore worldStore, ILogger<World> logger)
{
    private readonly Dictionary<RoomId, Room> _rooms = [];
    private readonly ConcurrentDictionary<Player, Room> _playerLocations = [];

    public ConcurrentDictionary<string, Player> Players { get; } = [];
    public DayPeriod CurrentDayPeriod { get; set; } = DayPeriod.Morning;
    public IReadOnlyDictionary<RoomId, Room> Rooms => _rooms;

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

    public Room GetDefaultRoom()
    {
        return Rooms.First()
            .Value;
    }

    public Room? GetPlayerLocation(Player player)
    {
        _ = _playerLocations.TryGetValue(player, out var room);

        return room;
    }

    public void MovePlayer(Player player, Room destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        _playerLocations.TryGetValue(player, out var origin);

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

        Players.TryRemove(player.Connection.Id, out var _);
    }

    public IReadOnlyCollection<Player> GetActivePlayers()
    {
        return _playerLocations.Keys.ToList();
    }

    public async Task<Room> CreateRoomAsync(string slug, string name, string description, string longDescription,
        string enterText, string exitText, string? creatorUsername, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        var room = new Room
        {
            Id = new(slug),
            Name = name,
            Description = description,
            LongDescription = longDescription,
            EnterText = enterText,
            ExitText = exitText,
            CreatorUsername = creatorUsername
        };

        _rooms[room.Id] = room;

        await worldStore.SaveRoomAsync(room, cancellationToken);

        logger.LogInformation("Room {RoomName} ({RoomId}) created", room.Name, room.Id);

        return room;
    }

    public async Task RenameRoomAsync(Room room, string name, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(room);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var oldName = room.Name;
        room.Name = name;

        await worldStore.RenameRoomAsync(room.Id, name, cancellationToken);

        logger.LogInformation("Room {OldName} ({RoomId}) renamed to {NewName}", oldName, room.Id, name);
    }

    public async Task RenameObjectAsync(Object item, string name, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var oldName = item.Name;
        item.Name = name;

        await worldStore.RenameObjectAsync(item.Id, name, cancellationToken);

        logger.LogInformation("Object {OldName} ({ObjectId}) renamed to {NewName}", oldName, item.Id, name);
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

    public bool SpawnTreasureInEmptyRoom(IReadOnlyList<Object> treasurePool)
    {
        ArgumentNullException.ThrowIfNull(treasurePool);

        if (treasurePool.Count == 0)
        {
            return false;
        }

        var emptyRooms = _rooms.Values
            .Where(r => r.PlayersInRoom.Count == 0)
            .ToList();

        if (emptyRooms.Count == 0)
        {
            logger.LogDebug("No empty rooms available for treasure spawn");
            return false;
        }

        var room = emptyRooms[Random.Shared.Next(emptyRooms.Count)];
        var treasure = treasurePool[Random.Shared.Next(treasurePool.Count)];

        treasure.MoveTo(room);

        logger.LogInformation("Spawned {TreasureName} in room {RoomName}", treasure.Name, room.Name);

        return true;
    }
}
