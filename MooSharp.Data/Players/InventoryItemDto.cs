namespace MooSharp.Data.Players;

public sealed record InventoryItemDto(
    string Id,
    string Name,
    string Description,
    string? TextContent,
    int Flags,
    string? KeyId,
    string? CreatorUsername);
