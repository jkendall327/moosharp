using System.Text;

namespace MooSharp;

public class PlayerMultiplexer
{
    private readonly List<PlayerConnection> _connections = new();

    public void AddPlayer(PlayerConnection player) => _connections.Add(player);

    public async Task SendMessage(Player player, string message, CancellationToken cancellationToken = default)
    {
        var conn = _connections.SingleOrDefault(s => s.PlayerObject.Equals(player));

        if (conn == null)
        {
            return;
        }

        await conn.SendMessageAsync(message, cancellationToken);
    }

    public async Task SendMessage(Player player, StringBuilder message, CancellationToken cancellationToken = default)
    {
        var conn = _connections.SingleOrDefault(s => s.PlayerObject.Equals(player));

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
                     .Where(s => s.PlayerObject.CurrentLocation is not null)
                     .Where(s => s.PlayerObject.CurrentLocation!.Equals(player.CurrentLocation))
                     .Where(s => s.PlayerObject != player)
                     .ToList();
        
        var tasks = others.Select(s => SendMessage(s.PlayerObject, message, cancellationToken));
        
        await Task.WhenAll(tasks);
    }
}