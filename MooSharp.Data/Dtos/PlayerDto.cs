namespace MooSharp.Data.Dtos;

public sealed record PlayerDto(
    Guid Id,
    string Username,
    string CurrentLocation,
    List<InventoryItemDto> Inventory);

public sealed record PlayerSnapshotDto(
    string Username,
    string CurrentLocation,
    List<InventoryItemDto> Inventory);

public sealed record NewPlayerRequest(
    Guid Id,
    string Username,
    string Password,
    string CurrentLocation);
