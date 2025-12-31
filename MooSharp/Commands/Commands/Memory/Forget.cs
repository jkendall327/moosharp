using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;

namespace MooSharp.Commands.Commands.Memory;

public class ForgetCommand : CommandBase<ForgetCommand>
{
    public required Player Player { get; init; }
    public required int Index { get; init; }
}

public class ForgetCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["forget"];
    public CommandCategory Category => CommandCategory.Utility;
    public string Description => "Forget a memory. Usage: forget <index>.";
    public ICommand Create(Player player, string args)
    {
        if (int.TryParse(args, out var i))
            return new ForgetCommand { Player = player, Index = i };

        // This is a bit hacky, but better than throwing or returning null if parsing fails
        // The handler will check bounds anyway.
        return new ForgetCommand { Player = player, Index = -1 };
    }
}

public class ForgetHandler : IHandler<ForgetCommand>
{
    public Task<CommandResult> Handle(ForgetCommand cmd, CancellationToken ct = default)
    {
        var i = cmd.Index - 1; // 1-based index
        if (i < 0 || i >= cmd.Player.Memories.Count)
            return Task.FromResult(CommandResult.Failure("That memory doesn't exist."));

        cmd.Player.Memories.RemoveAt(i);
        return Task.FromResult(new CommandResult().Add(cmd.Player, new MemoryForgottenEvent()));
    }
}

public record MemoryForgottenEvent : IGameEvent;

public class MemoryForgottenEventFormatter : IGameEventFormatter<MemoryForgottenEvent>
{
    public string FormatForActor(MemoryForgottenEvent e) => "You tear the page out of your notebook.";
    public string FormatForObserver(MemoryForgottenEvent e) => string.Empty;
}
