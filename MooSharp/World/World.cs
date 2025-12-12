using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using MooSharp.Actors.Players;
using MooSharp.Actors.Rooms;
using MooSharp.Data.Worlds;
using MooSharp.Features.WorldClock;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.World;

public class World(IWorldRepository worldRepository, ILogger<World> logger)
{
    private readonly Dictionary<RoomId, Room> _rooms = [];
    private readonly ConcurrentDictionary<Player, Room> _playerLocations = [];

    private ConcurrentDictionary<Guid, Player> Players { get; } = [];
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

    public void RegisterPlayer(Player player)
    {
        Players[player.Id.Value] = player;
    }

    public Player? TryGetPlayer(Guid id)
    {
        _ = Players.TryGetValue(id, out var player);

        return player;
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

    public Room GetLocationOrThrow(Player player)
    {
        return GetPlayerLocation(player) ??
               throw new InvalidOperationException($"Player {player.Username} has no location.");
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

        Players.TryRemove(player.Id.Value, out var _);
    }

    public IReadOnlyCollection<Player> GetActivePlayers()
    {
        return _playerLocations.Keys.ToList();
    }

    public async Task<Room> CreateRoomAsync(string slug,
        string name,
        string description,
        string longDescription,
        string enterText,
        string exitText,
        string? creatorUsername,
        CancellationToken cancellationToken = default)
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

        await worldRepository.SaveRoomAsync(WorldSnapshotFactory.CreateSnapshot(room), cancellationToken: cancellationToken);

        logger.LogInformation("Room {RoomName} ({RoomId}) created", room.Name, room.Id);

        return room;
    }

    public async Task RenameRoomAsync(Room room, string name, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(room);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var oldName = room.Name;
        room.Name = name;

        await worldRepository.RenameRoomAsync(room.Id.Value, name, cancellationToken: cancellationToken);

        logger.LogInformation("Room {OldName} ({RoomId}) renamed to {NewName}", oldName, room.Id, name);
    }

    public async Task RenameObjectAsync(Object item, string name, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var oldName = item.Name;
        item.Name = name;

        await worldRepository.RenameObjectAsync(item.Id.Value.ToString(), name, cancellationToken: cancellationToken);

        logger.LogInformation("Object {OldName} ({ObjectId}) renamed to {NewName}", oldName, item.Id, name);
    }

    public async Task AddExitAsync(Room origin,
        Room destination,
        string direction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentException.ThrowIfNullOrWhiteSpace(direction);

        origin.Exits.RemoveAll(e => e.Name.Equals(direction, StringComparison.OrdinalIgnoreCase));

        var exit = new Exit
        {
            Name = direction,
            Description = $"An exit leading to {destination.Name}.",
            Destination = destination.Id,
            Aliases = [direction[0].ToString()],
            Keywords = [],
            IsOpen = true,
            CanBeLocked = false
        };

        origin.Exits.Add(exit);

        var snapshot = new ExitSnapshotDto(
            exit.Id,
            exit.Name,
            exit.Description,
            exit.Destination.Value,
            exit.IsHidden,
            exit.IsLocked,
            exit.IsOpen,
            exit.CanBeOpened,
            exit.CanBeLocked,
            exit.KeyId,
            exit.Aliases.ToList(),
            exit.Keywords.ToList());

        await worldRepository.SaveExitAsync(origin.Id.Value, snapshot, cancellationToken: cancellationToken);
    }

    public async Task UpdateRoomDescriptionAsync(Room room,
        string description,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(room);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        room.Description = description;
        room.LongDescription = description;

        await worldRepository.UpdateRoomDescriptionAsync(room.Id.Value, description, description, cancellationToken: cancellationToken);

        logger.LogInformation("Room {RoomName} ({RoomId}) description updated", room.Name, room.Id);
    }

    public void SpawnTreasureInEmptyRoom(IReadOnlyList<Object> treasurePool)
    {
        ArgumentNullException.ThrowIfNull(treasurePool);

        if (treasurePool.Count == 0)
        {
            return;
        }

        var emptyRooms = _rooms
            .Values
            .Where(r => r.PlayersInRoom.Count == 0)
            .ToList();

        if (emptyRooms.Count == 0)
        {
            logger.LogDebug("No empty rooms available for treasure spawn");

            return;
        }

        var room = emptyRooms[Random.Shared.Next(emptyRooms.Count)];
        var treasure = treasurePool[Random.Shared.Next(treasurePool.Count)];

        treasure.MoveTo(room);

        logger.LogInformation("Spawned {TreasureName} in room {RoomName}", treasure.Name, room.Name);
    }

    public IReadOnlyCollection<RoomSnapshotDto> CreateSnapshot()
    {
        var rooms = Rooms
            .Values
            .ToList();

        return WorldSnapshotFactory.CreateSnapshots(rooms);
    }

    /// <summary>
    /// Marks a room as modified so it will be saved to the database.
    /// Use this when objects in the room have been created, modified, or deleted.
    /// </summary>
    public void MarkRoomModified(Room room)
    {
        ArgumentNullException.ThrowIfNull(room);

        // Queue the room for saving via the background service
        _ = worldRepository.SaveRoomAsync(WorldSnapshotFactory.CreateSnapshot(room));

        logger.LogDebug("Room {RoomName} marked as modified", room.Name);
    }
}