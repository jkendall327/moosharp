using System.Text;

namespace MooSharp;

public class PlayerGameLoopManager(CommandParser parser, CommandExecutor executor, PlayerMultiplexer multiplexer)
{
    public async Task OnPlayerInput(InputReceivedEvent e)
    {
        var player = e.Player;
        var command = e.Input;
        var token = e.Token;
        
        var cmd = await parser.ParseAsync(player, command, token);
                
        var sb = new StringBuilder();
                
        await executor.Handle(cmd, sb, token);
                
        await BuildCurrentRoomDescription(player, sb);
                
        await multiplexer.SendMessage(player, sb, token);
    }
    
    private static async Task BuildCurrentRoomDescription(Player player, StringBuilder sb)
    {
        if (player.CurrentLocation == null)
        {
            throw new InvalidOperationException("Current location not set");
        }

        var room = await player.CurrentLocation.Ask(new RequestMessage<Room, Room>(Task.FromResult));

        sb.AppendLine(room.Description);

        var availableExits = await player.GetCurrentlyAvailableExitsAsync();

        var availableExitsMessage = $"Available exits: {string.Join(", ", availableExits.Select(s => s.Key))}";

        sb.AppendLine(availableExitsMessage);
    }
}