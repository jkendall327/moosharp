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
    
    public async Task BuildCurrentRoomDescription(PlayerActor player, StringBuilder sb)
    {
        var room = await player.QueryAsync(s => s.CurrentLocation);

        sb.AppendLine(room.Description);

        var players = await room.QueryAsync(s => s.PlayersInRoom);
        
        foreach (var playerActor in players.Except([player]))
        {
            sb.AppendLine($"{playerActor.Username} is here.");
        }

        var availableExits = await player.GetCurrentlyAvailableExitsAsync();

        var availableExitsMessage = $"Available exits: {string.Join(", ", availableExits.Select(s => s.Key))}";

        sb.AppendLine(availableExitsMessage);
    }
}