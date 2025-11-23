using System.Text;
using Microsoft.Extensions.Logging;

namespace MooSharp;

public class MoveCommand : ICommand
{
    public required PlayerActor Player { get; init; }
    public required string TargetExit { get; init; }

    public string BroadcastMessage(string username) => $"{username} went to {TargetExit}";
}

public class MoveHandler(PlayerMultiplexer multiplexer, ILogger<MoveHandler> logger) : IHandler<MoveCommand>
{
    public async Task Handle(MoveCommand cmd, StringBuilder buffer, CancellationToken cancellationToken = default)
    {
        var player = cmd.Player;

        var current = await player.QueryAsync(s => s.CurrentLocation);

        var exits = await player.GetCurrentlyAvailableExitsAsync();

        if (!exits.TryGetValue(cmd.TargetExit, out var exit))
        {
            buffer.AppendLine("That exit doesn't exist.");
            return;
        }

        buffer.AppendLine($"You head to {exit.Description}");

        var broadcastMessage = cmd.BroadcastMessage(player.Username);

        await multiplexer.SendToAllInRoomExceptPlayer(player, new(broadcastMessage), cancellationToken);

        // There is a known potential race condition here.
        // Removing the player from one room and adding them to the other room is non-atomic.
        // Probably fine for a toy game for now...
        await player.MoveTo(exit);
        await current.RemovePlayer(player);
        await exit.AddPlayer(player);

        await multiplexer.SendToAllInRoomExceptPlayer(player, new($"{player.Username} arrived"), cancellationToken);

        logger.LogDebug("Move complete");
    }
}
