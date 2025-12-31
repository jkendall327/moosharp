using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;

namespace MooSharp.Commands.Commands.Meta;

public class HelpCommand : CommandBase<HelpCommand>
{
    public required Player Player { get; init; }
    public string? Topic { get; init; }
}

public class HelpCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["/help", "help", "commands"];
    public string Description => "List available commands and their usage.";
    public CommandCategory Category => CommandCategory.Meta;

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        string? topic = null;
        if (!ctx.IsFinished)
        {
            topic = ctx.GetRemainingText();
        }

        command = new HelpCommand
        {
            Player = ctx.Player,
            Topic = topic
        };
        return null;
    }
}

public class HelpHandler(CommandReference commandReference) : IHandler<HelpCommand>
{
    public Task<CommandResult> Handle(HelpCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();

        if (!string.IsNullOrWhiteSpace(cmd.Topic))
        {
            var specificHelp = commandReference.GetHelpForCommand(cmd.Topic);
            if (specificHelp is not null)
            {
                result.Add(cmd.Player, new SystemMessageEvent(specificHelp));
            }
            else
            {
                result.Add(cmd.Player, new SystemMessageEvent("Command not found."));
            }
        }
        else
        {
            var helpMessage = commandReference.BuildHelpText();
            result.Add(cmd.Player, new SystemMessageEvent(helpMessage));
        }

        return Task.FromResult(result);
    }
}