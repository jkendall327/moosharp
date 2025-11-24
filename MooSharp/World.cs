namespace MooSharp;

public class World
{
    public Dictionary<string, Player> Players { get; } = [];
    public IReadOnlyDictionary<RoomId, Room> Rooms => _rooms;
    private readonly Dictionary<RoomId, Room> _rooms;

    private readonly Dictionary<Player, Room> _playerLocations = [];

    public World(IEnumerable<Room> rooms)
    {
        ArgumentNullException.ThrowIfNull(rooms);

        _rooms = rooms.ToDictionary(r => r.Id);
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
}
