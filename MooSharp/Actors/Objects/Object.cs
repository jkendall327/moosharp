using System.Collections.Frozen;
using MooSharp.Actors.Players;
using MooSharp.Actors.Rooms;

namespace MooSharp.Actors.Objects;

public readonly record struct ObjectId(Guid Value)
{
    public static ObjectId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public class Object
{
    public ObjectId Id { get; init; } = ObjectId.New();
    public required string Name { get; set; }
    public required string Description { get; init; }
    public string? CreatorUsername { get; init; }
    public IReadOnlyCollection<string> Keywords { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase).ToFrozenSet();
    public string? TextContent { get; private set; }
    public ObjectFlags Flags { get; set; } = ObjectFlags.None;
    public string? KeyId { get; init; }
    public decimal Value { get; init; }

    public IContainer? Container { get; private set; }

    public Player? Owner => Container as Player;
    public Room? Location => Container as Room;

    public bool IsOwnedBy(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        return string.Equals(CreatorUsername, player.Username, StringComparison.OrdinalIgnoreCase);
    }

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

    public bool IsScenery
    {
        get => Flags.HasFlag(ObjectFlags.Scenery);
        set
        {
            if (value)
            {
                SetFlag(ObjectFlags.Scenery, true);
            }

            SetFlag(ObjectFlags.Scenery, value);
        }
    }

    private string? GetStateSummary()
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

        return parts.Any() ? $"It is {string.Join(" and ", parts)}." : null;
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
