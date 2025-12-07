using MooSharp.Actors.Rooms;
using MooSharp.Data.Players;

namespace MooSharp.Actors.Players;

public static class PlayerSnapshotFactory
{
    public static PlayerSnapshotDto CreateSnapshot(Player player, Room currentLocation)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(currentLocation);

        return new(player.Username, currentLocation.Id.Value, CreateInventorySnapshot(player));
    }

    private static List<InventoryItemDto> CreateInventorySnapshot(Player player)
    {
        return player.Inventory
            .Select(o => new InventoryItemDto(
                o.Id.Value.ToString(),
                o.Name,
                o.Description,
                o.TextContent,
                (int)o.Flags,
                o.KeyId,
                o.CreatorUsername))
            .ToList();
    }
}
