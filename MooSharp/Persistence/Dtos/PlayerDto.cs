using MooSharp.Actors;

namespace MooSharp.Persistence.Dtos;

public class PlayerDto
{
    public RoomId CurrentLocation { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
    public List<InventoryItemDto> Inventory { get; set; } = [];
}