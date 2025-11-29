using System.Collections.Frozen;

namespace MooSharp;

public readonly record struct ObjectId(Guid Value)
{
    public static ObjectId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

[Flags]
public enum ObjectFlags
{
    None = 0,
    Takeable = 1 << 0,
    Container = 1 << 1,
    Openable = 1 << 2,
    Open = 1 << 3,
    Lockable = 1 << 4,
    Locked = 1 << 5,
    Scenery = 1 << 6,
    LightSource = 1 << 7
}

public class Object
{
    public ObjectId Id { get; init; } = ObjectId.New();
    public required string Name { get; init; }
    public required string Description { get; init; }
    public IReadOnlyCollection<string> Keywords { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase).ToFrozenSet();
    public string? TextContent { get; private set; }
    public ObjectFlags Flags { get; set; } = ObjectFlags.None;
    public string? KeyId { get; set; }

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

    public void WriteText(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        TextContent = text.Trim();
    }

    public bool IsTakeable
    {
        get => Flags.HasFlag(ObjectFlags.Takeable);
        set => SetFlag(ObjectFlags.Takeable, value);
    }

    public bool IsContainer
    {
        get => Flags.HasFlag(ObjectFlags.Container);
        set => SetFlag(ObjectFlags.Container, value);
    }

    public bool IsOpenable
    {
        get => Flags.HasFlag(ObjectFlags.Openable);
        set
        {
            SetFlag(ObjectFlags.Openable, value);

            if (!value)
            {
                SetFlag(ObjectFlags.Open, false);
            }
        }
    }

    public bool IsOpen
    {
        get => Flags.HasFlag(ObjectFlags.Open);
        set
        {
            if (value)
            {
                SetFlag(ObjectFlags.Openable, true);
            }

            SetFlag(ObjectFlags.Open, value);
        }
    }

    public bool IsLockable
    {
        get => Flags.HasFlag(ObjectFlags.Lockable);
        set
        {
            SetFlag(ObjectFlags.Lockable, value);

            if (!value)
            {
                SetFlag(ObjectFlags.Locked, false);
            }
        }
    }

    public bool IsLocked
    {
        get => Flags.HasFlag(ObjectFlags.Locked);
        set
        {
            if (value)
            {
                SetFlag(ObjectFlags.Lockable, true);
            }

            SetFlag(ObjectFlags.Locked, value);
        }
    }

    public string? GetStateSummary()
    {
        var parts = new List<string>();

        if (IsOpenable)
        {
            parts.Add(IsOpen ? "open" : "closed");
        }

        if (IsLockable)
        {
            parts.Add(IsLocked ? "locked" : "unlocked");
        }

        if (!parts.Any())
        {
            return null;
        }

        return $"It is {string.Join(" and ", parts)}.";
    }

    public string DescribeWithState()
    {
        var state = GetStateSummary();

        return string.IsNullOrWhiteSpace(state) ? Description : $"{Description} ({state})";
    }

    private void SetFlag(ObjectFlags flag, bool value)
    {
        if (value)
        {
            Flags |= flag;
            return;
        }

        Flags &= ~flag;
    }

    public override string ToString() => Name;
}
