namespace MooSharp;

public class Room
{
    public int Id { get; init; }
    public string Name { get; set; } = "An empty space";
    public string Description { get; init; } = "It's a featureless, empty room.";
    public Dictionary<string, ObjectActor> Contents { get; } = new();
    public Dictionary<string, RoomActor> Exits { get; } = new();
}

public class RoomActor(Room state) : Actor<Room>(state);