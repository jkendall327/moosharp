using System.Collections.Frozen;

namespace MooSharp;

public readonly record struct ObjectId(Guid Value)
{
    public static ObjectId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public class Object
{
    public ObjectId Id { get; init; } = ObjectId.New();
    public required string Name { get; init; }
    public required string Description { get; init; }
    public IReadOnlyCollection<string> Keywords { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase).ToFrozenSet(); 

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