using MooSharp.Actors;

namespace MooSharp.Persistence.Dtos;

public class InventoryItemDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public string? TextContent { get; init; }
    public ObjectFlags Flags { get; init; } = ObjectFlags.None;
    public string? KeyId { get; init; }
    public string? CreatorUsername { get; init; }
}
