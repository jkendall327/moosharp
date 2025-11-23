using System.Text;
using Microsoft.Extensions.Logging;
using MooSharp.Messaging;

namespace MooSharp;

public class MoveCommand : ICommand
{
    public required Player Player { get; init; }
    public required string TargetExit { get; init; }

    public string BroadcastMessage(string username) => $"{username} went to {TargetExit}";
}

public class MoveHandler(ILogger<MoveHandler> logger) : IHandler<MoveCommand>
{
    public Task<CommandResult> Handle(MoveCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var player = cmd.Player;
        
        var exits = player.CurrentLocation.Exits;

        if (!exits.TryGetValue(cmd.TargetExit, out var exit))
        {
            result.Add(player, "That exit doesn't exist.");
            return Task.FromResult(result);
        }

        result.Add(player, $"You head to {exit.Description}.");

        var broadcastMessage = cmd.BroadcastMessage(player.Username);

        result.BroadcastToAllButPlayer(player, broadcastMessage);
        
        player.CurrentLocation.PlayersInRoom.Remove(player);
        player.CurrentLocation = exit;
        player.CurrentLocation.PlayersInRoom.Add(player);
        
        result.BroadcastToAllButPlayer(player, $"{player.Username} arrived");

        logger.LogDebug("Move complete");

        return Task.FromResult(result);
    }
}
