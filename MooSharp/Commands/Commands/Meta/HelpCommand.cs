using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Presentation;

namespace MooSharp.Commands.Commands.Meta;

public class HelpCommand : CommandBase<HelpCommand>
{
    public required Player Player { get; init; }
}

public class HelpCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["/help", "help", "commands"];

    public string Description => "List available commands and their usage.";

    public ICommand Create(Player player, string args)
        => new HelpCommand
        {
            Player = player
        };

    public CommandCategory Category => CommandCategory.Meta;
}

public class HelpHandler(CommandReference commandReference) : IHandler<HelpCommand>
{
    public Task<CommandResult> Handle(HelpCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();

        var helpMessage = commandReference.BuildHelpText();

        result.Add(cmd.Player, new SystemMessageEvent(helpMessage));

        return Task.FromResult(result);
    }
}
