namespace MooSharp.Messaging;

public enum MessageAudience
{
    Actor,
    Observer
}

public interface IGameEvent;

public record GameMessage(Player Player, IGameEvent Event, MessageAudience Audience = MessageAudience.Actor);

public class CommandResult
{
    // Messages to be sent out
    public List<GameMessage> Messages { get; } = new();

    // Helper to add a message to a specific player
    public void Add(Player player, IGameEvent @event, MessageAudience audience = MessageAudience.Actor)
    {
        Messages.Add(new GameMessage(player, @event, audience));
    }

    // Helper to broadcast to a room (excluding specific people usually)
    public void Broadcast(Room room, IGameEvent @event, MessageAudience audience = MessageAudience.Observer, params Player[] exclude)
    {
        foreach (var player in room.PlayersInRoom)
        {
            if (!exclude.Contains(player))
            {
                Messages.Add(new (player, @event, audience));
            }
        }
    }

    public void BroadcastToAllButPlayer(Player player, IGameEvent @event, MessageAudience audience = MessageAudience.Observer)
    {
        Broadcast(player.CurrentLocation, @event, audience, exclude: player);
    }
}