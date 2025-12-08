using JetBrains.Annotations;
using MoonSharp.Interpreter;
using MooSharp.Actors.Players;

namespace MooSharp.Scripting.Api;

[MoonSharpUserData]
public class LuaGameApi(ScriptExecutionContext context)
{
    private readonly List<ScriptMessage> _messages = [];

    [UsedImplicitly]
    public void Tell(string playerName, string message)
    {
        var player = FindPlayer(playerName);
        if (player is not null)
        {
            _messages.Add(new(player, message));
        }
    }

    [UsedImplicitly]
    public void TellRoom(string message)
    {
        foreach (var player in context.Location.PlayersInRoom)
        {
            _messages.Add(new(player, message));
        }
    }

    [UsedImplicitly]
    public void TellRoomExcept(string message, string excludePlayerName)
    {
        foreach (var player in context.Location.PlayersInRoom)
        {
            if (!string.Equals(player.Username, excludePlayerName, StringComparison.OrdinalIgnoreCase))
            {
                _messages.Add(new(player, message));
            }
        }
    }

    public IReadOnlyList<ScriptMessage> GetMessages() => _messages;

    private Player? FindPlayer(string name)
    {
        return context.Location.PlayersInRoom
            .FirstOrDefault(p => string.Equals(p.Username, name, StringComparison.OrdinalIgnoreCase));
    }
}
