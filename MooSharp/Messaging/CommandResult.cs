namespace MooSharp.Messaging;

public record GameMessage(Player Player, string Content);

public class CommandResult
{
    // Messages to be sent out
    public List<GameMessage> Messages { get; } = new();

    // Helper to add a message to a specific player
    public void Add(Player player, string content) 
    {
        Messages.Add(new GameMessage(player, content));
    }

    // Helper to broadcast to a room (excluding specific people usually)
    public void Broadcast(Room room, string content, params Player[] exclude)
    {
        foreach (var player in room.PlayersInRoom)
        {
            if (!exclude.Contains(player))
            {
                Messages.Add(new (player, content));
            }
        }
    }

    public void BroadcastToAllButPlayer(Player player, string content)
    {
        Broadcast(player.CurrentLocation, content, exclude: player);
    }
}