using JetBrains.Annotations;
using MooSharp.Actors;

namespace MooSharp.Persistence.Dtos;

[UsedImplicitly]
public class ObjectDto
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public string? RoomSlug { get; set; }
    public string? TextContent { get; set; }
    public ObjectFlags Flags { get; set; } = ObjectFlags.None;
    public string? KeyId { get; set; }
    public string? CreatorUsername { get; set; }
}
