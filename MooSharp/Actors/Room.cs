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

public class Room
{
    public RoomId Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public List<Object> Contents { get; } = new();
    public Dictionary<string, RoomId> Exits { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<Player> PlayersInRoom { get; } = new();

    public Object? FindObject(string keyword)
    {
        // Simple fuzzy match
        return Contents.FirstOrDefault(o =>
            o.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) || o.Keywords.Contains(keyword));
    }

    public override string ToString() => Id.ToString();
}