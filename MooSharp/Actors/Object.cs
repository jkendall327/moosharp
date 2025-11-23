using Microsoft.Extensions.Logging;

namespace MooSharp;

public class Object
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }

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

    private Room? _location;

    public Room? Location
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