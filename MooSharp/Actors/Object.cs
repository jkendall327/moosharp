using Microsoft.Extensions.Logging;

namespace MooSharp;

public class Object
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }

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

public class ObjectActor(Object state, ILoggerFactory factory) : Actor<Object>(state, factory)
{
    public int Id => _state.Id;
    public string Name => _state.Name; 
    public string Description =>  _state.Description; 
}