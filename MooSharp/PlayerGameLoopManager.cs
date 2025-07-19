using System.Text;

namespace MooSharp;

public class PlayerGameLoopManager(CommandParser parser, CommandExecutor executor, PlayerMultiplexer multiplexer)
{
    public async Task RunMainLoopAsync(PlayerConnection conn, CancellationToken token = default)
    {
        await conn.SendMessageAsync("Welcome to the C# MOO!", token);

        while (!token.IsCancellationRequested)
        {
            try
            {
                var command = await conn.GetStringAsync(token);

                if (command == null)
                {
                    // Client disconnected
                    break;
                }

                var player = conn.Player;
                
                var cmd = await parser.ParseAsync(player, command, token);

                var sb = new StringBuilder();
                
                await executor.Handle(cmd, sb, token);

                await BuildCurrentRoomDescription(player, sb);

                await multiplexer.SendMessage(player, sb, token);
            }
            catch (IOException)
            {
                break;
            }
        }
    }
    
    private static async Task BuildCurrentRoomDescription(Player player, StringBuilder sb)
    {
        if (player.CurrentLocation == null)
        {
            throw new InvalidOperationException("Current location not set");
        }

        var room = await player.CurrentLocation.Ask(new RequestMessage<Room, Room>(Task.FromResult));

        sb.AppendLine(room.Description);

        var availableExits = player.GetCurrentlyAvailableExits().Select(s => s.Key).ToArray();

        var availableExitsMessage = $"Available exits: {string.Join(", ", availableExits)}";

        sb.AppendLine(availableExitsMessage);
    }
}