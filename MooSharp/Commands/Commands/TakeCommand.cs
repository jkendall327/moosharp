using MooSharp.Messaging;

namespace MooSharp;

public class TakeCommand : CommandBase<TakeCommand>
{
    public required Player Player { get; init; }
    public required string Target { get; init; }
}

public class TakeCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } =
    [
        "take", "grab", "get"
    ];

    public ICommand Create(Player player, string args) =>
        new TakeCommand
        {
            Player = player,
            Target = args
        };
}

public class TakeHandler : IHandler<TakeCommand>
{
    public Task<CommandResult> Handle(TakeCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var player = cmd.Player;

        var o = player.CurrentLocation.FindObject(cmd.Target);

        if (o is null)
        {
            result.Add(player, $"There is no {cmd.Target} here.");

            return Task.FromResult(result);
        }

        if (o.Owner is null)
        {
            player.CurrentLocation.Contents.Remove(o);
            o.Owner = player;
            player.Inventory.Add(o.Name, o);
        }
        else if (o.Owner == player)
        {
            result.Add(player, $"You take the {o.Name}.");
        }
        else
        {
            result.Add(player, $"Someone else already has the {o.Name}!");
        }

        return Task.FromResult(result);
    }
}