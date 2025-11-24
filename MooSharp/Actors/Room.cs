using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;

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
    public required string Description { get; init; }
    private readonly List<Object> _contents = new();
    public IReadOnlyCollection<Object> Contents => _contents;
    public Dictionary<string, RoomId> Exits { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<Player> PlayersInRoom { get; } = new();

    public Object? FindObject(string keyword)
    {
        return this.FindObjectInContainer(keyword);
    }

    public string DescribeFor(Player player)
    {
        var sb = new StringBuilder();

        sb.AppendLine(Description);

        var otherPlayers = PlayersInRoom.Select(s => s.Username).Except([player.Username]);

        sb.AppendLine($"{string.Join(", ", otherPlayers)} are here.");

        var availableExits = Exits.Select(s => s.Key);
        var availableExitsMessage = $"Available exits: {string.Join(", ", availableExits)}";

        sb.AppendLine(availableExitsMessage);

        return sb.ToString();
    }

    public override string ToString() => Id.ToString();

    IReadOnlyCollection<Object> IContainer.Contents => _contents;

    void IContainer.AddToContents(Object item) => _contents.Add(item);

    bool IContainer.RemoveFromContents(Object item) => _contents.Remove(item);
}