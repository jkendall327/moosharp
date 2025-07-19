using System.Text;

namespace MooSharp;

public class TakeCommand : ICommand
{
    public required Player Player { get; init; }
    public required string Target { get; init; }
}

public class TakeHandler : IHandler<TakeCommand>
{
    public Task Handle(TakeCommand cmd, StringBuilder buffer, CancellationToken cancellationToken = default)
    {
        if (cmd.Player.CurrentLocation == null)
        {
            throw new InvalidOperationException("Player must have a location.");
        }
        
        var player = cmd.Player;

        var contents = player.CurrentLocation.QueryState(s => s.Contents);

        if (contents.TryGetValue(cmd.Target, out var o))
        {
            o.Post(new ActionMessage<Object>(obj => TakeOwnership(buffer, obj, player)));
            
        }
        else
        {
            buffer.AppendLine($"There is no {cmd.Target} here.");
        }

        return Task.CompletedTask;
    }

    private static Task TakeOwnership(StringBuilder buffer, Object obj, Player player)
    {
        if (obj.Owner is null)
        {
            obj.Owner = player;
            buffer.AppendLine($"You picked up the {obj.Description}.");
        }
        else
        {
            buffer.AppendLine($"Someone else picked up the {obj.Description} first.");
        }

        return Task.CompletedTask;
    }
}