using MooSharp.Messaging;

namespace MooSharp;

public class ExamineCommand : CommandBase<ExamineCommand>
{
    public required Player Player { get; init; }
    public required string Target { get; init; }
}

public class ExamineCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["examine", "view", "look"];

    public ICommand Create(Player player, string args)
        => new ExamineCommand
        {
            Player = player,
            Target = args
        };
}

public class ExamineHandler : IHandler<ExamineCommand>
{
    public Task<CommandResult> Handle(ExamineCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();

        var player = cmd.Player;

        if (string.IsNullOrWhiteSpace(cmd.Target))
        {
            throw new NotImplementedException(
                "When no target is specified for 'examine', just print the room's description");
        }

        if (cmd.Target is "me")
        {
            var inventory = player.Inventory
                .Select(s => s.Value)
                .ToList();

            result.Add(player, new SelfExaminedEvent(player, inventory));
        }

        var current = player.CurrentLocation;

        var obj = player.CurrentLocation.FindObject(cmd.Target);

        if (obj is not null)
        {
            result.Add(player, new ObjectExaminedEvent(obj));
        }

        return Task.FromResult(result);
    }
}