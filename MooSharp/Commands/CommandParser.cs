using System.Text;
using Microsoft.Extensions.Logging;

namespace MooSharp;

public class CommandParser(World world, PlayerMultiplexer multiplexer, CommandExecutor executor, ILogger<CommandParser> logger)
{
    public async Task<ICommand?> ParseAsync(PlayerActor player, string command, CancellationToken token = default)
    {
        logger.LogDebug("Parsing player input: {Input}", command);
        
        var split = command.Split(' ');

        var verb = split.FirstOrDefault();

        // TODO: handle split.count > 2.
        
        ICommand? cmd = verb switch
        {
            "move" => new MoveCommand
            {
                Player = player,
                Origin = player.CurrentLocation,
                TargetExit = split.Last()
            },
            "examine" => new ExamineCommand
            {
                Player = player,
                Target = split.Last()
            },
            "take" => new TakeCommand
            {
                Player = player,
                Target = split.Last()
            },
            _ => null
        };

        return cmd;
    }
}