namespace MooSharp.Persistence;

public class InventoryItemDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public string? TextContent { get; init; }
}
