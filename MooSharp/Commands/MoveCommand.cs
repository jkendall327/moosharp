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
            var name = await exit.QueryAsync(s => s.Description);

            buffer.AppendLine($"You head to {name}");
            
            var username = await player.QueryAsync(s => s.Username);
            
            var broadcastMessage = cmd.BroadcastMessage(username);

            await multiplexer.SendToAllInRoomExceptPlayer(player, new(broadcastMessage), cancellationToken);

            player.Post(new ActionMessage<Player>(s =>
            {
                s.CurrentLocation = exit;
                return Task.CompletedTask;
            }));
            
            current.Post(new ActionMessage<Room>(r =>
            {
                r.PlayersInRoom.Remove(player);
                return Task.CompletedTask;
            }));
            
            exit.Post(new ActionMessage<Room>(r =>
            {
                r.PlayersInRoom.Add(player);
                return Task.CompletedTask;
            }));
        }
        else
        {
            buffer.AppendLine("That exit doesn't exist.");
        }
    }
}