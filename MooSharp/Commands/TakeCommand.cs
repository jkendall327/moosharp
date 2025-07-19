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
            obj.Location?.Post(new ActionMessage<Room>(s =>
            {
                s.Contents.Remove(obj.Name);

                return Task.CompletedTask;
            }));

            obj.Owner = player;

            buffer.AppendLine($"You picked up the {obj.Name}.");
        }
        else if (obj.Owner.Equals(player))
        {
            buffer.AppendLine($"You already have the {obj.Name}.");
        }
        else
        {
            buffer.AppendLine($"There is no {obj.Name} here.");
        }

        return Task.CompletedTask;
    }
}