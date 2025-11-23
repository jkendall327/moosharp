using MooSharp.Messaging;

namespace MooSharp;

public class ExamineCommand : ICommand
{
    public required Player Player { get; init; }
    public required string Target { get; init; }
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
            result.Add(player, "You took a look at yourself. You're looking pretty good.");

            var descriptions = player.Inventory
                .Select(s => s.Value.Description)
                .ToList();

            if (descriptions.Any())
            {
                result.Add(player, "You have:");

                foreach (var se in descriptions)
                {
                    result.Add(player, se);
                }
            }
        }

        var current = player.CurrentLocation;

        var obj = player.CurrentLocation.FindObject(cmd.Target);
        
        if (obj is not null)
        {
            result.Add(player, obj.Description);
        }

        return Task.FromResult(result);
    }
}