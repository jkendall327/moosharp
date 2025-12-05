using JetBrains.Annotations;
using MooSharp.Actors;

namespace MooSharp.World.Dtos;

[UsedImplicitly]
public class ObjectDto
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public ObjectFlags Flags { get; set; }
    public string? TextContent { get; set; }
    public RoomId? RoomSlug { get; set; }
    public string? KeyId { get; set; }
    public string? CreatorUsername { get; set; }
}
