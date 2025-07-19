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
        
        await conn.SendMessageAsync(message,  cancellationToken);
    }
}