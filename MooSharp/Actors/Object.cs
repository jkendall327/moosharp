namespace MooSharp;

public class Object
{
    public int Id { get; init; }
    public string Name { get; set; } = "An empty space";
    public string Description { get; set; } = "It's a featureless, empty room.";

    private PlayerActor? _owner;

    public PlayerActor? Owner
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
    
    public override string ToString() => Name;
}

public class ObjectActor(Object state) : Actor<Object>(state);