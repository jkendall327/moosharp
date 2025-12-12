namespace MooSharp.Data.Players;

public sealed record PlayerDto(
    Guid Id,
    string Username,
    string Description,
    string CurrentLocation,
    List<InventoryItemDto> Inventory);

public sealed record PlayerSnapshotDto(
    string Username,
    string Description,
    string CurrentLocation,
    List<InventoryItemDto> Inventory);

public sealed record NewPlayerRequest(
    Guid Id,
    string Username,
    string Password,
    string CurrentLocation,
    string Description);
