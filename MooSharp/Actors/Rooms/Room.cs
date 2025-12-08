using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MooSharp.Actors.Players;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Actors.Rooms;

public readonly record struct RoomId(string Value)
{
    public override string ToString() => Value;
    public static implicit operator RoomId(string value) => new(value);
}

public class RoomIdJsonConverter : JsonConverter<RoomId>
{
    public override RoomId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, RoomId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}

public class Room : IContainer
{
    public RoomId Id { get; init; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string LongDescription { get; set; }
    public required string EnterText { get; init; }
    public required string ExitText { get; init; }
    public string? CreatorUsername { get; init; }
    private readonly List<Object> _contents = [];
    private readonly List<Player> _playersInRoom = [];
    public IReadOnlyCollection<Object> Contents => _contents;
    public List<Exit> Exits { get; } = [];
    public IReadOnlyCollection<Player> PlayersInRoom => _playersInRoom;

    public string DescribeFor(Player player, bool useLongDescription = false)
    {
        var sb = new StringBuilder();

        sb.AppendLine(useLongDescription ? LongDescription : Description);

        foreach (var obj in Contents)
        {
            sb.AppendLine($"A {obj.Name} is here.");
        }

        var otherPlayers = PlayersInRoom
            .Select(s => s.Username)
            .Except([player.Username])
            .ToList();

        if (otherPlayers.Count is 1)
        {
            sb.AppendLine($"{otherPlayers.Single()} is here.");
        }
        if (otherPlayers.Count > 1)
        {
            sb.AppendLine($"{string.Join(", ", otherPlayers)} are here.");
        }

        var visibleExits = Exits
            .Where(e => !e.IsHidden)
            .ToList();

        var availableExitsMessage = visibleExits.Any()
            ? $"Visible exits: {string.Join(", ", visibleExits.Select(DescribeExit))}"
            : "No visible exits.";

        sb.AppendLine(availableExitsMessage);

        return sb.ToString();
    }

    public bool IsOwnedBy(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        return string.Equals(CreatorUsername, player.Username, StringComparison.OrdinalIgnoreCase);
    }

    public override string ToString() => Id.ToString();

    private static string DescribeExit(Exit exit)
    {
        var state = exit.IsOpen ? "Open" : "Closed";

        if (exit is { IsLocked: true, IsOpen: false })
        {
            state = "Locked";
        }

        return $"[[{exit.Name}]] ({state})";
    }

    IReadOnlyCollection<Object> IContainer.Contents => _contents;

    void IContainer.AddToContents(Object item) => _contents.Add(item);

    void IContainer.RemoveFromContents(Object item) => _contents.Remove(item);

    internal void AddPlayer(Player player)
    {
        if (_playersInRoom.Contains(player))
        {
            return;
        }

        _playersInRoom.Add(player);
    }

    internal void RemovePlayer(Player player)
    {
        _playersInRoom.Remove(player);
    }
}