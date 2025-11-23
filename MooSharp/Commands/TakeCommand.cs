using MooSharp.Messaging;

namespace MooSharp;

public class TakeCommand : ICommand
{
    public required Player Player { get; init; }
    public required string Target { get; init; }
}

public class TakeHandler : IHandler<TakeCommand>
{
    public Task<CommandResult> Handle(TakeCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var player = cmd.Player;

        var contents = player.CurrentLocation.Contents;

        if (!contents.TryGetValue(cmd.Target, out var o))
        {
            result.Add(player, $"There is no {cmd.Target} here.");

            return Task.FromResult(result);
        }

        if (o.Owner is null)
        {
            contents.Remove(o.Name);
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