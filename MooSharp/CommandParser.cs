using System.Text;

namespace MooSharp;

public class CommandParser(World world, PlayerMultiplexer multiplexer)
{
    public async Task ParseAndExecuteAsync(Player player, string command, CancellationToken token = default)
    {
        var sb = new StringBuilder();

        player.CurrentLocation ??= world.Rooms.First();

        var split = command.Split(' ');

        if (split.First() == "move")
        {
            var cmd = new MoveCommand
            {
                Player = player,
                Origin = player.CurrentLocation,
                TargetExit = split.Last()
            };
            
            // post the command, get the stringbuilder from the response...
        }
        else
        {
            sb.AppendLine("That command wasn't recognized. Use 'move' to go between locations.");
        }

        var room = await player.CurrentLocation.Ask(new RequestMessage<Room, Room>(Task.FromResult));
        
        sb.AppendLine(room.Description);

        var availableExits = player.GetCurrentlyAvailableExits()
                                   .Select(s => s.Key)
                                   .ToArray();
        
        var availableExitsMessage = $"Available exits: {string.Join(", ", availableExits)}";

        sb.AppendLine(availableExitsMessage);

        await multiplexer.SendMessage(player, sb, token);
    }
}