namespace MooSharp;

public class Object
{
    public int Id { get; init; }
    public string Name { get; set; } = "An empty space";
    public string Description { get; set; } = "It's a featureless, empty room.";

    private Player? _owner;

    public Player? Owner
    {
        get => _owner;
        set
        {
            _owner = value;

            if (value is not null)
            {
                Location = null;
            }
        }
    }

    private RoomActor? _location;

    public RoomActor? Location
    {
        get => _location;
        set
        {
            _location = value;

            if (value is not null)
            {
                Owner = null;
            }
        }
    }
}

public class ObjectActor(Object state) : Actor<Object>(state);