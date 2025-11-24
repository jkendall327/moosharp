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

    public IContainer? Container { get; private set; }

    public Player? Owner => Container as Player;
    public Room? Location => Container as Room;

    public void MoveTo(IContainer destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        if (ReferenceEquals(Container, destination))
        {
            return;
        }

        Container?.RemoveFromContents(this);

        destination.AddToContents(this);
        Container = destination;
    }

    public override string ToString() => Name;
}