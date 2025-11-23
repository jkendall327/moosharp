using Microsoft.Extensions.Logging;

namespace MooSharp;

/// <summary>
/// Parses raw text into commands.
/// </summary>
public class CommandParser(ILogger<CommandParser> logger)
{
    public Task<ICommand?> ParseAsync(Player player, string command, CancellationToken token = default)
    {
        logger.LogDebug("Parsing player input: {Input}", command);
        
        var split = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var verb = split.FirstOrDefault();
        var targetWords = split.Skip(1);
        var target = string.Join(" ", targetWords);

        ICommand? cmd = verb switch
        {
            "move" or "go" or "walk" => new MoveCommand
            {
                Player = player,
                TargetExit = target
            },
            "examine" or "view" or "look" => new ExamineCommand
            {
                Player = player,
                Target = target
            },
            "take" or "grab" or "get" => new TakeCommand
            {
                Player = player,
                Target = target
            },
            _ => null
        };

        return Task.FromResult(cmd);
    }
}