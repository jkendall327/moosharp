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

        var description = player.CurrentLocation.QueryState(s => s.Description);
        
        sb.AppendLine(description);

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