using System.Text;

namespace MooSharp;

public class TakeCommand : ICommand
{
    public required PlayerActor Player { get; init; }
    public required string Target { get; init; }
}

public class TakeHandler : IHandler<TakeCommand>
{
    public async Task Handle(TakeCommand cmd, StringBuilder buffer, CancellationToken cancellationToken = default)
    {
        var player = cmd.Player;

        var location = await player.GetCurrentRoomAsync();
        
        var contents = await location.QueryAsync(s => s.Contents);

        if (contents.TryGetValue(cmd.Target, out var o))
        {
            o.Post(new ActionMessage<Object>(obj => TakeOwnership(buffer, o, obj, player)));
        }
        else
        {
            buffer.AppendLine($"There is no {cmd.Target} here.");
        }
    }

    private static Task TakeOwnership(StringBuilder buffer, ObjectActor o, Object obj, PlayerActor player)
    {
        if (obj.Owner is null)
        {
            obj.Location?.Post(new ActionMessage<Room>(s =>
            {
                s.Contents.Remove(obj.Name);

                return Task.CompletedTask;
            }));

            obj.Owner = player;
            
            player.Post(new ActionMessage<Player>(s =>
            {
                s.Inventory.Add(obj.Name, o);
                return Task.CompletedTask;
            }));

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