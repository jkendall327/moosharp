using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MooSharp;

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
    public required string Name { get; init; }
    public required string Description { get; set; }
    public required string LongDescription { get; set; }
    public required string EnterText { get; init; }
    public required string ExitText { get; init; }
    private readonly List<Object> _contents = new();
    private readonly List<Player> _playersInRoom = new();
    public IReadOnlyCollection<Object> Contents => _contents;
    public Dictionary<string, RoomId> Exits { get; } = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyCollection<Player> PlayersInRoom => _playersInRoom;

    public string DescribeFor(Player player, bool useLongDescription = false)
    {
        var sb = new StringBuilder();

        sb.AppendLine(useLongDescription ? LongDescription : Description);

        foreach (var obj in Contents)
        {
            sb.AppendLine($"{obj.Name} is here.");
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

        var availableExits = Exits.Select(s => s.Key);
        var availableExitsMessage = $"Available exits: {string.Join(", ", availableExits)}";

        sb.AppendLine(availableExitsMessage);

        return sb.ToString();
    }

    public override string ToString() => Id.ToString();

    IReadOnlyCollection<Object> IContainer.Contents => _contents;

    void IContainer.AddToContents(Object item) => _contents.Add(item);

    bool IContainer.RemoveFromContents(Object item) => _contents.Remove(item);

    internal bool AddPlayer(Player player)
    {
        if (_playersInRoom.Contains(player))
        {
            return false;
        }

        _playersInRoom.Add(player);

        return true;
    }

    internal bool RemovePlayer(Player player) => _playersInRoom.Remove(player);
}