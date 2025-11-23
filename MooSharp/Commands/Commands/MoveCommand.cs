using Microsoft.Extensions.Logging;
using MooSharp.Messaging;

namespace MooSharp;

public class MoveCommand : CommandBase<MoveCommand>
{
    public required Player Player { get; init; }
    public required string TargetExit { get; init; }

}

public class MoveCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["move", "go", "walk"];

    public ICommand Create(Player player, string args)
        => new MoveCommand
        {
            Player = player,
            TargetExit = args
        };
}

public class MoveHandler(World world, ILogger<MoveHandler> logger) : IHandler<MoveCommand>
{
    public Task<CommandResult> Handle(MoveCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var player = cmd.Player;
        
        var exits = player.CurrentLocation.Exits;

        if (!exits.TryGetValue(cmd.TargetExit, out var exit))
        {
            result.Add(player, new ExitNotFoundEvent(cmd.TargetExit));
            return Task.FromResult(result);
        }

        var originRoom = player.CurrentLocation;
        var exitRoom = world.Rooms[exit];

        result.Add(player, new PlayerMovedEvent(player, exitRoom));

        result.BroadcastToAllButPlayer(player, new PlayerDepartedEvent(player, originRoom, cmd.TargetExit));

        originRoom.PlayersInRoom.Remove(player);
        player.CurrentLocation = exitRoom;
        player.CurrentLocation.PlayersInRoom.Add(player);

        result.BroadcastToAllButPlayer(player, new PlayerArrivedEvent(player, exitRoom));

        logger.LogDebug("Move complete");

        return Task.FromResult(result);
    }
}
