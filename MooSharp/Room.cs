namespace MooSharp;

public class Room
{
    public int Id { get; init; }
    public string Name { get; set; } = "An empty space";
    public string Description { get; set; } = "It's a featureless, empty room.";
    public Dictionary<string, string> Contents { get; } = new();
    public Dictionary<string, RoomActor> Exits { get; } = new();
}

public class RoomActor : Actor<Room>
{
    public RoomActor(Room state) : base(state) { }
}