using System.Collections.Generic;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace MooSharp.Actors.Rooms;

[UsedImplicitly]
public class RoomDto
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string LongDescription { get; set; }
    public required string EnterText { get; set; }
    public required string ExitText { get; set; }
    public string? CreatorUsername { get; set; }

    [JsonConverter(typeof(RoomIdJsonConverter))]
    public required RoomId Slug { get; set; }
    public List<ExitDto> Exits { get; set; } = [];
}

public class ExitDto
{
    public required string Direction { get; set; }
    public required string DestinationSlug { get; set; }
    public required string Description { get; set; }
    public bool IsHidden { get; set; }
    public bool IsLocked { get; set; }
    public bool IsOpen { get; set; } = true;
    public string? KeyId { get; set; }
    public List<string> Aliases { get; set; } = [];
    public List<string> Keywords { get; set; } = [];
}
