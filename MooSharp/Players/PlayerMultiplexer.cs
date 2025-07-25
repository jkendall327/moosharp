using System.Collections.Concurrent;
using System.Text;

namespace MooSharp;

public class PlayerMultiplexer
{
    public readonly ConcurrentDictionary<string, IPlayerConnection> _connections = new();

    public void AddPlayer(IPlayerConnection streamBasedPlayer) =>
        _connections.TryAdd(streamBasedPlayer.Id, streamBasedPlayer);

    public void RemovePlayer(IPlayerConnection streamBasedPlayer)
    {
        _connections.Remove(streamBasedPlayer.Id, out var _);
    }
    
    public IPlayerConnection? TryGetPlayer(string id) => _connections.GetValueOrDefault(id);

    public async Task SendMessage(PlayerActor player, string message, CancellationToken cancellationToken = default)
    {
        var conn = _connections.SingleOrDefault(s => s.Value.Player.Equals(player))
                               .Value;

        if (conn == null)
        {
            return;
        }

        await conn.SendMessageAsync(message, cancellationToken);
    }

    public async Task SendMessage(PlayerActor player,
        StringBuilder message,
        CancellationToken cancellationToken = default)
    {
        var conn = _connections.SingleOrDefault(s => s.Value.Player.Equals(player))
                               .Value;

        if (conn == null)
        {
            return;
        }

        await conn.SendMessageAsync(message, cancellationToken);
    }

    public async Task SendToAllInRoomExceptPlayer(PlayerActor player,
        StringBuilder message,
        CancellationToken cancellationToken = default)
    {
        var playerLocation = await player.QueryAsync(s => s.CurrentLocation);

        var all = await playerLocation.QueryAsync(s => s.PlayersInRoom);

        var others = all.Where(s => s != player);
        
        var tasks = others.Select(p => SendMessage(p, message, cancellationToken));
        
        await Task.WhenAll(tasks);
    }
}