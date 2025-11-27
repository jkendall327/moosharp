namespace MooSharp;

public static class AccessibleObjectSearcher
{
    public static SearchResult FindNearbyObject(Player player, Room room, string target)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(room);
        ArgumentException.ThrowIfNullOrWhiteSpace(target);

        var inventorySearch = player.Inventory.FindObjects(target);

        if (inventorySearch.Status is not SearchStatus.NotFound)
        {
            return inventorySearch;
        }

        return room.FindObjects(target);
    }
}
