using MooSharp.Actors;
using MooSharp.Persistence.Dtos;

namespace MooSharp.Persistence;

public static class PlayerSnapshotFactory
{
    public static PlayerDto CreateNewPlayerDto(Player player, Room currentLocation, string hashedPassword)
    {
        return new PlayerDto
        {
            Username = player.Username,
            Password = hashedPassword,
            CurrentLocation = currentLocation.Id,
            Inventory = CreateInventorySnapshot(player)
        };
    }

    public static PlayerSnapshotDto CreateSnapshot(Player player, Room currentLocation)
    {
        return new PlayerSnapshotDto
        {
            Username = player.Username,
            CurrentLocation = currentLocation.Id,
            Inventory = CreateInventorySnapshot(player)
        };
    }

    public static List<InventoryItemDto> CreateInventorySnapshot(Player player)
    {
        return player.Inventory
            .Select(o => new InventoryItemDto
            {
                Id = o.Id.Value.ToString(),
                Name = o.Name,
                Description = o.Description,
                TextContent = o.TextContent,
                Flags = o.Flags,
                KeyId = o.KeyId,
                CreatorUsername = o.CreatorUsername
            })
            .ToList();
    }
}
