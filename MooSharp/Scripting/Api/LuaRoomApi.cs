using MoonSharp.Interpreter;
using MooSharp.Actors.Rooms;

namespace MooSharp.Scripting.Api;

[MoonSharpUserData]
public class LuaRoomApi
{
    private readonly Room _room;

    public LuaRoomApi(Room room)
    {
        _room = room;
    }

    public string Name => _room.Name;
    public string Description => _room.Description;

    public string[] GetPlayers()
    {
        return _room.PlayersInRoom.Select(p => p.Username).ToArray();
    }

    public string[] GetObjects()
    {
        return _room.Contents.Select(o => o.Name).ToArray();
    }

    public bool HasObject(string objectName)
    {
        return _room.Contents.Any(obj =>
            string.Equals(obj.Name, objectName, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasPlayer(string playerName)
    {
        return _room.PlayersInRoom.Any(p =>
            string.Equals(p.Username, playerName, StringComparison.OrdinalIgnoreCase));
    }

    // Full manipulation methods - will be fully implemented in Phase 4
    public bool MoveObject(string objectName, string destination)
    {
        // TODO: Implement in Phase 4
        return false;
    }
}
