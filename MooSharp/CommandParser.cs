namespace MooSharp;

public class CommandParser(World world, PlayerMultiplexer multiplexer)
{
    public async Task ParseAndExecuteAsync(Player player, string command, CancellationToken token = default)
    {
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
                await multiplexer.SendMessage(player, "That exit doesn't exist.", token);
            }
        }
        else
        {
            await multiplexer.SendMessage(player, "That command wasn't recognized. Use 'move' to go between locations.", token);
        }

        var description = player.CurrentLocation.QueryState(s => s.Description);

        var availableExits = GetCurrentlyAvailableExits(player).Select(s => s.Key).ToArray();

        var availableExitsMessage = $"Available exits: {string.Join(", ", availableExits)}";

        await multiplexer.SendMessage(player, description, token);
        await multiplexer.SendMessage(player, availableExitsMessage, token);
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