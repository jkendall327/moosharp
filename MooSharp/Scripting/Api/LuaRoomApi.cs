using JetBrains.Annotations;
using MoonSharp.Interpreter;
using MooSharp.Actors.Players;
using MooSharp.Actors.Rooms;

namespace MooSharp.Scripting.Api;

[MoonSharpUserData]
public class LuaRoomApi(Room room)
{
    [UsedImplicitly]
    public string Name => room.Name;

    [UsedImplicitly]
    public string Description => room.Description;

    [UsedImplicitly]
    public string[] GetPlayers()
    {
        return room.PlayersInRoom.Select(p => p.Username).ToArray();
    }

    [UsedImplicitly]
    public string[] GetObjects()
    {
        return room.Contents.Select(o => o.Name).ToArray();
    }

    [UsedImplicitly]
    public bool HasObject(string objectName)
    {
        return room.Contents.Any(obj =>
            string.Equals(obj.Name, objectName, StringComparison.OrdinalIgnoreCase));
    }

    [UsedImplicitly]
    public bool HasPlayer(string playerName)
    {
        return room.PlayersInRoom.Any(p =>
            string.Equals(p.Username, playerName, StringComparison.OrdinalIgnoreCase));
    }

    [UsedImplicitly]
    public bool MoveObject(string objectName, string playerName)
    {
        var obj = room.Contents.FirstOrDefault(o =>
            string.Equals(o.Name, objectName, StringComparison.OrdinalIgnoreCase));

        if (obj is null)
        {
            return false;
        }

        var player = room.PlayersInRoom.FirstOrDefault(p =>
            string.Equals(p.Username, playerName, StringComparison.OrdinalIgnoreCase));

        if (player is null)
        {
            return false;
        }

        obj.MoveTo(player);
        return true;
    }
}
