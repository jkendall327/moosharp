using MooSharp.Actors;

namespace MooSharp.Persistence.Dtos;

public class PlayerSnapshotDto
{
    public RoomId CurrentLocation { get; init; }
    public required string Username { get; init; }
    public List<InventoryItemDto> Inventory { get; init; } = [];
}
