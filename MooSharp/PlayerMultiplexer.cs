using System.Collections.Concurrent;
using System.Text;

namespace MooSharp;

public class PlayerMultiplexer
{
    private readonly ConcurrentDictionary<Guid, PlayerConnection> _connections = new();

    public void AddPlayer(PlayerConnection player) => _connections.TryAdd(player.Id, player);
    public bool RemovePlayer(PlayerConnection player) => _connections.Remove(player.Id, out _);

    public async Task SendMessage(Player player, string message, CancellationToken cancellationToken = default)
    {
        var conn = _connections.SingleOrDefault(s => s.Value.Player.Equals(player)).Value;

        if (conn == null)
        {
            return;
        }

        await conn.SendMessageAsync(message, cancellationToken);
    }

    public async Task SendMessage(Player player, StringBuilder message, CancellationToken cancellationToken = default)
    {
        var conn = _connections.SingleOrDefault(s => s.Value.Player.Equals(player)).Value;

        if (conn == null)
        {
            return;
        }

        await conn.SendMessageAsync(message, cancellationToken);
    }

    public async Task SendToAllInRoomExceptPlayer(Player player,
        StringBuilder message,
        CancellationToken cancellationToken = default)
    {
        if (player.CurrentLocation is null)
        {
            return;
        }

        var others = _connections
                     .Select(s => s.Value)
                     .Where(s => s.Player.CurrentLocation is not null)
                     .Where(s => s.Player.CurrentLocation!.Equals(player.CurrentLocation))
                     .Where(s => !s.Player.Equals(player))
                     .ToList();
        
        var tasks = others.Select(s => SendMessage(s.Player, message, cancellationToken));
        
        await Task.WhenAll(tasks);
    }
}