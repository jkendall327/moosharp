using System.Text;

namespace MooSharp;

public class MoveCommand : ICommand
{
    public required Player Player { get; set; }
    public required RoomActor Origin { get; set; }
    public required string TargetExit { get; set; }

    public string BroadcastMessage() => $"{Player.Username} went to {TargetExit}";
}

public class MoveHandler(PlayerMultiplexer multiplexer) : IHandler<MoveCommand>
{
    public async Task Handle(MoveCommand cmd, StringBuilder buffer, CancellationToken cancellationToken = default)
    {
        var player = cmd.Player;

        var exits = player.GetCurrentlyAvailableExits();

        if (exits.TryGetValue(cmd.TargetExit, out var exit))
        {
            player.CurrentLocation = exit;

            buffer.AppendLine($"You head to {exit}");
            
            var broadcastMessage = cmd.BroadcastMessage();

            await multiplexer.SendToAllInRoomExceptPlayer(player, new(broadcastMessage), cancellationToken);
        }
        else
        {
            buffer.AppendLine("That exit doesn't exist.");
        }
    }
}