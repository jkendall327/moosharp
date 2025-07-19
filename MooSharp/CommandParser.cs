namespace MooSharp;

public class CommandParser(World world, PlayerMultiplexer multiplexer)
{
    public async Task ParseAndExecuteAsync(Player player, string command, CancellationToken token = default)
    {
        player.CurrentLocation ??= world.Rooms.First();

        if (command == "move")
        {
            // player.CurrentLocation;
        }

        await multiplexer.SendMessage(player, "test" + command, token);
    }
}