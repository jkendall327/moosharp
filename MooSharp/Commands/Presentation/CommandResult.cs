using MooSharp.Actors;

namespace MooSharp.Messaging;

public class CommandResult
{
    // Messages to be sent out
    public List<GameMessage> Messages { get; } = [];

    // Helper to add a message to a specific player
    public void Add(Player player, IGameEvent @event, MessageAudience audience = MessageAudience.Actor)
    {
        Messages.Add(new(player, @event, audience));
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
}