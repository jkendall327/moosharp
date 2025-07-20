using Microsoft.Extensions.Logging;

namespace MooSharp;

public class CommandParser(ILogger<CommandParser> logger)
{
    public Task<ICommand?> ParseAsync(PlayerActor player, string command, CancellationToken token = default)
    {
        logger.LogDebug("Parsing player input: {Input}", command);
        
        var split = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var verb = split.FirstOrDefault();
        var targetWords = split.Skip(1);
        var target = string.Join(" ", targetWords);

        ICommand? cmd = verb switch
        {
            "move" => new MoveCommand
            {
                Player = player,
                TargetExit = target
            },
            "examine" => new ExamineCommand
            {
                Player = player,
                Target = target
            },
            "take" => new TakeCommand
            {
                Player = player,
                Target = target
            },
            _ => null
        };

        return Task.FromResult(cmd);
    }
}