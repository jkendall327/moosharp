using MooSharp.Actors;
using MooSharp.Data.Dtos;

namespace MooSharp.Data.Mapping;

public static class PlayerSnapshotFactory
{
    public static NewPlayerRequest CreateNewPlayer(Player player, Room currentLocation, string password)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(currentLocation);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        return new(player.Username, password, currentLocation.Id.Value, CreateInventorySnapshot(player));
    }

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
