using System.Collections.Concurrent;
using System.Text;

namespace MooSharp;

public class PlayerMultiplexer
{
    public readonly ConcurrentDictionary<Guid, IPlayerConnection> _connections = new();

    public void AddPlayer(IPlayerConnection streamBasedPlayer) =>
        _connections.TryAdd(streamBasedPlayer.Id, streamBasedPlayer);

    public bool RemovePlayer(IPlayerConnection streamBasedPlayer) => _connections.Remove(streamBasedPlayer.Id, out _);

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

        var others = _connections
                     .Select(s => s.Value)
                              .Where(s => !s.Player.Equals(player));

        var tasks = others.Select(SendIfInSameRoom);
        
        await Task.WhenAll(tasks);

        return;
        
        async Task SendIfInSameRoom(IPlayerConnection playerConnection)
        {
            var theirLocation = await playerConnection.Player.QueryAsync(s => s.CurrentLocation);

            if (playerLocation == theirLocation)
            {
                await SendMessage(playerConnection.Player, message, cancellationToken);
            }
        }
    }
}