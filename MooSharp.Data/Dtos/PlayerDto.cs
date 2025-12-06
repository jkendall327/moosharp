namespace MooSharp.Data.Dtos;

public sealed record PlayerDto(
    string Username,
    string Password,
    string CurrentLocation,
    List<InventoryItemDto> Inventory);

public sealed record PlayerSnapshotDto(
    string Username,
    string CurrentLocation,
    List<InventoryItemDto> Inventory);

public sealed record NewPlayerRequest(
    string Username,
    string Password,
    string CurrentLocation);
