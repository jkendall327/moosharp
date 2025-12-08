using JetBrains.Annotations;
using MoonSharp.Interpreter;
using MooSharp.Actors.Players;
using MooSharp.Actors.Rooms;

namespace MooSharp.Scripting.Api;

[MoonSharpUserData]
public class LuaActorApi(Player player, Room room)
{
    [UsedImplicitly]
    public string Name => player.Username;

    [UsedImplicitly]
    public bool HasItem(string itemName)
    {
        return player.Inventory.Any(obj =>
            string.Equals(obj.Name, itemName, StringComparison.OrdinalIgnoreCase));
    }

    [UsedImplicitly]
    public string[] GetInventory()
    {
        return player.Inventory.Select(obj => obj.Name).ToArray();
    }

    [UsedImplicitly]
    public bool GiveItem(string itemName)
    {
        var obj = room.Contents.FirstOrDefault(o =>
            string.Equals(o.Name, itemName, StringComparison.OrdinalIgnoreCase));

        if (obj is null)
        {
            return false;
        }

        obj.MoveTo(player);
        return true;
    }

    [UsedImplicitly]
    public bool TakeItem(string itemName)
    {
        var obj = player.Inventory.FirstOrDefault(o =>
            string.Equals(o.Name, itemName, StringComparison.OrdinalIgnoreCase));

        if (obj is null)
        {
            return false;
        }

        obj.MoveTo(room);
        return true;
    }
}
