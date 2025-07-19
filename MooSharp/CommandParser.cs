using System.Text;

namespace MooSharp;

public class CommandParser(World world, PlayerMultiplexer multiplexer)
{
    public async Task ParseAndExecuteAsync(Player player, string command, CancellationToken token = default)
    {
        var sb = new StringBuilder();

        player.CurrentLocation ??= world.Rooms.First();

        var split = command.Split(' ');

        var exits = GetCurrentlyAvailableExits(player);

        if (split.First() == "move")
        {
            var target = split.Last();

            if (exits.TryGetValue(target, out var exit))
            {
                var cmd = new MoveCommand
                {
                    TargetExit = target
                };

                var broadcastMessage = cmd.BroadcastMessage(player);
                
                await multiplexer.SendToAllInRoomExceptPlayer(player, new(broadcastMessage), token);

                player.CurrentLocation = exit;
            }
            else
            {
                sb.AppendLine("That exit doesn't exist.");
            }
        }
        else
        {
            sb.AppendLine("That command wasn't recognized. Use 'move' to go between locations.");
        }

        var room = await player.CurrentLocation.Ask(new RequestMessage<Room, Room>(Task.FromResult));
        
        sb.AppendLine(room.Description);

        var availableExits = GetCurrentlyAvailableExits(player).Select(s => s.Key).ToArray();
        var availableExitsMessage = $"Available exits: {string.Join(", ", availableExits)}";

        sb.AppendLine(availableExitsMessage);

        await multiplexer.SendMessage(player, sb, token);
    }

    private Dictionary<string, RoomActor> GetCurrentlyAvailableExits(Player player)
    {
        if (player.CurrentLocation == null)
        {
            return new();
        }

        return player.CurrentLocation.QueryState(s => s.Exits);
    }
}