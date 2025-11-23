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

    public string Description => "Move to an adjacent room. Usage: move <exit>.";

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

        var originRoom = world.GetPlayerLocation(player)
            ?? throw new InvalidOperationException("Player has no known current location.");

        var exits = originRoom.Exits;

        if (!exits.TryGetValue(cmd.TargetExit, out var exit))
        {
            result.Add(player, new ExitNotFoundEvent(cmd.TargetExit));
            return Task.FromResult(result);
        }

        var exitRoom = world.Rooms[exit];

        result.Add(player, new PlayerMovedEvent(player, exitRoom));

        result.BroadcastToAllButPlayer(originRoom, player, new PlayerDepartedEvent(player, originRoom, cmd.TargetExit));

        world.MovePlayer(player, exitRoom);

        var description = exitRoom.DescribeFor(player);
        result.Add(player, new RoomDescriptionEvent(description));

        result.BroadcastToAllButPlayer(exitRoom, player, new PlayerArrivedEvent(player, exitRoom));

        logger.LogDebug("Move complete");

        return Task.FromResult(result);
    }
}
