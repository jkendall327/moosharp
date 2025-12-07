using MoonSharp.Interpreter;
using MooSharp.Actors.Players;

namespace MooSharp.Scripting.Api;

[MoonSharpUserData]
public class LuaActorApi
{
    private readonly Player _player;

    public LuaActorApi(Player player)
    {
        _player = player;
    }

    public string Name => _player.Username;

    public bool HasItem(string itemName)
    {
        return _player.Inventory.Any(obj =>
            string.Equals(obj.Name, itemName, StringComparison.OrdinalIgnoreCase));
    }

    public string[] GetInventory()
    {
        return _player.Inventory.Select(obj => obj.Name).ToArray();
    }

    // Full manipulation methods - will be fully implemented in Phase 4
    public bool GiveItem(string itemName)
    {
        // TODO: Implement in Phase 4 - requires access to object creation/world
        return false;
    }

    public bool TakeItem(string itemName)
    {
        // TODO: Implement in Phase 4 - requires proper item removal
        return false;
    }
}
