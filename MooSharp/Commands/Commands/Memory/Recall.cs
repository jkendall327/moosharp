using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;

namespace MooSharp.Commands.Commands.Memory;

public class RecallCommand : CommandBase<RecallCommand>
{
    public required Player Player { get; init; }
    public int? Index { get; init; }
}

public class RecallCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["recall"];
    public CommandCategory Category => CommandCategory.Utility;
    public string Description => "Recall memories. Usage: recall [index].";
    public ICommand Create(Player player, string args)
    {
        int? index = null;
        if (int.TryParse(args, out var i))
            index = i;

        return new RecallCommand { Player = player, Index = index };
    }
}

public class RecallHandler : IHandler<RecallCommand>
{
    public Task<CommandResult> Handle(RecallCommand cmd, CancellationToken ct = default)
    {
        var memories = cmd.Player.Memories;
        if (memories.Count == 0)
            return Task.FromResult(CommandResult.Failure("You haven't remembered anything."));

        if (cmd.Index.HasValue)
        {
            var i = cmd.Index.Value - 1; // 1-based index for user
            if (i < 0 || i >= memories.Count)
                return Task.FromResult(CommandResult.Failure($"Memory #{cmd.Index} not found."));

            return Task.FromResult(new CommandResult().Add(cmd.Player, new SingleMemoryRecalledEvent(memories[i], cmd.Index.Value)));
        }

        return Task.FromResult(new CommandResult().Add(cmd.Player, new MemoriesRecalledEvent(memories)));
    }
}

public record SingleMemoryRecalledEvent(string Memory, int Index) : IGameEvent;
public record MemoriesRecalledEvent(List<string> Memories) : IGameEvent;

public class SingleMemoryRecalledEventFormatter : IGameEventFormatter<SingleMemoryRecalledEvent>
{
    public string FormatForActor(SingleMemoryRecalledEvent e) => $"Memory #{e.Index}: {e.Memory}";
    public string FormatForObserver(SingleMemoryRecalledEvent e) => string.Empty;
}

public class MemoriesRecalledEventFormatter : IGameEventFormatter<MemoriesRecalledEvent>
{
    public string FormatForActor(MemoriesRecalledEvent e)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Your memories:");
        for (var i = 0; i < e.Memories.Count; i++)
        {
            sb.AppendLine($"{i + 1}. {e.Memories[i]}");
        }
        return sb.ToString().TrimEnd();
    }
    public string FormatForObserver(MemoriesRecalledEvent e) => string.Empty;
}
