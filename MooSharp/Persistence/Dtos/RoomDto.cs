using System.Text.Json.Serialization;

namespace MooSharp;

public class RoomDto
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string LongDescription { get; set; }
    public required string EnterText { get; set; }
    public required string ExitText { get; set; }

    [JsonConverter(typeof(RoomIdJsonConverter))]
    public required RoomId Slug { get; set; }
    public IReadOnlyList<string> ConnectedRooms { get; set; } = [];
}