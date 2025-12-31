using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;

namespace MooSharp.Commands.Commands.Memory;

public class RememberCommand : CommandBase<RememberCommand>
{
    public required Player Player { get; init; }
    public required string Text { get; init; }
}

public class RememberCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["remember"];
    public CommandCategory Category => CommandCategory.Utility;
    public string Description => "Adds a note to your memories. Usage: remember <text>.";
    public ICommand Create(Player player, string args) => new RememberCommand { Player = player, Text = args };
}

public class RememberHandler : IHandler<RememberCommand>
{
    public Task<CommandResult> Handle(RememberCommand cmd, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cmd.Text))
            return Task.FromResult(CommandResult.Failure("What do you want to remember?"));

        cmd.Player.Memories.Add(cmd.Text);
        return Task.FromResult(new CommandResult().Add(cmd.Player, new PlayerRememberedEvent(cmd.Player)));
    }
}

public record PlayerRememberedEvent(Player Player) : IGameEvent;

public class PlayerRememberedEventFormatter : IGameEventFormatter<PlayerRememberedEvent>
{
    public string FormatForActor(PlayerRememberedEvent e) => "You jot down a note.";
    public string FormatForObserver(PlayerRememberedEvent e) => $"{e.Player.Username} jots down a note.";
}
