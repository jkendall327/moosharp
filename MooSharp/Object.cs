namespace MooSharp;

public class Object
{
    public int Id { get; init; }
    public string Name { get; set; } = "An empty space";
    public string Description { get; set; } = "It's a featureless, empty room.";
    public Player? Owner { get; set; }
    public RoomActor? Location { get; set; }
}

public class ObjectActor(Object state) : Actor<Object>(state);