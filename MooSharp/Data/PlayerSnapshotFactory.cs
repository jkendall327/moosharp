using MooSharp.Actors;
using MooSharp.Data.Dtos;

namespace MooSharp.Data.Mapping;

public static class PlayerSnapshotFactory
{
    public static NewPlayerRequest CreateNewPlayer(string username, Room currentLocation, string password)
    {
        ArgumentNullException.ThrowIfNull(currentLocation);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        return new(username, password, currentLocation.Id.Value, []);
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
