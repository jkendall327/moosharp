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

    public void Broadcast(IEnumerable<Player> players, IGameEvent @event, MessageAudience audience = MessageAudience.Observer,
        params Player[] exclude)
    {
        var excluded = exclude.ToHashSet();

        foreach (var player in players)
        {
            if (!excluded.Contains(player))
            {
                Messages.Add(new(player, @event, audience));
            }
        }
    }

    public void BroadcastToAllButPlayer(Room room, Player player, IGameEvent @event, MessageAudience audience = MessageAudience.Observer)
    {
        Broadcast(room.PlayersInRoom, @event, audience, exclude: player);
    }

    public void BroadcastToAll(World world, IGameEvent @event, MessageAudience audience = MessageAudience.Observer,
        params Player[] exclude)
    {
        Broadcast(world.Players.Values, @event, audience, exclude);
    }
}