using MoonSharp.Interpreter;
using MooSharp.Actors.Rooms;

namespace MooSharp.Scripting.Api;

[MoonSharpUserData]
public class LuaRoomApi(Room room)
{
    public string Name => room.Name;
    public string Description => room.Description;

    public string[] GetPlayers()
    {
        return room.PlayersInRoom.Select(p => p.Username).ToArray();
    }

    public string[] GetObjects()
    {
        return room.Contents.Select(o => o.Name).ToArray();
    }

    public bool HasObject(string objectName)
    {
        return room.Contents.Any(obj =>
            string.Equals(obj.Name, objectName, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasPlayer(string playerName)
    {
        return room.PlayersInRoom.Any(p =>
            string.Equals(p.Username, playerName, StringComparison.OrdinalIgnoreCase));
    }

    // Full manipulation methods - will be fully implemented in Phase 4
    public bool MoveObject(string objectName, string destination)
    {
        // TODO: Implement in Phase 4
        return false;
    }
}
