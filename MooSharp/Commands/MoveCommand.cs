using System.Text;

namespace MooSharp;

public class MoveCommand : ICommand
{
    public required PlayerActor Player { get; set; }
    public required string TargetExit { get; set; }

    public string BroadcastMessage(string username) => $"{username} went to {TargetExit}";
}

public class MoveHandler(PlayerMultiplexer multiplexer) : IHandler<MoveCommand>
{
    public async Task Handle(MoveCommand cmd, StringBuilder buffer, CancellationToken cancellationToken = default)
    {
        var player = cmd.Player;

        var current = await player.QueryAsync(s => s.CurrentLocation);

        var exits = await player.GetCurrentlyAvailableExitsAsync();

        if (exits.TryGetValue(cmd.TargetExit, out var exit))
        {
            buffer.AppendLine($"You head to {exit.Description}");
            
            var broadcastMessage = cmd.BroadcastMessage(player.Username);

            await multiplexer.SendToAllInRoomExceptPlayer(player, new(broadcastMessage), cancellationToken);

            var playerMove = player.QueryAsync(s =>
            {
                s.CurrentLocation = exit;
                return true;
            });

            var roomLeave = current.QueryAsync(r =>
            {
                r.PlayersInRoom.Remove(player);
                return true;
            });

            var roomEnter = exit.QueryAsync(r =>
            {
                r.PlayersInRoom.Add(player);
                return true;
            });

            await Task.WhenAll(playerMove, roomLeave, roomEnter);
        }
        else
        {
            buffer.AppendLine("That exit doesn't exist.");
        }
    }
}
