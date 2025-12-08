using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Commands.Commands.Creative;

public class RecycleObjectCommand : CommandBase<RecycleObjectCommand>
{
    public required Player Player { get; init; }
    public required Object Target { get; init; }
}

public class RecycleObjectCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["@recycle"];
    public CommandCategory Category => CommandCategory.General;

    public string Description => "Permanently destroy an object you created. Usage: @recycle <object>.";

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        command = null;

        var bindResult = binder.BindNearbyObject(ctx);
        if (!bindResult.IsSuccess)
        {
            return bindResult.ErrorMessage;
        }

        command = new RecycleObjectCommand
        {
            Player = ctx.Player,
            Target = bindResult.Value!
        };

        return null;
    }
}

public record ObjectRecycledEvent(Player Player, Object Item) : IGameEvent;

public class ObjectRecycledEventFormatter : IGameEventFormatter<ObjectRecycledEvent>
{
    public string FormatForActor(ObjectRecycledEvent gameEvent) =>
        $"You recycle '{gameEvent.Item.Name}'. It vanishes in a puff of smoke.";

    public string? FormatForObserver(ObjectRecycledEvent gameEvent) =>
        $"{gameEvent.Player.Username} recycles '{gameEvent.Item.Name}'. It vanishes in a puff of smoke.";
}

public class RecycleObjectHandler(World.World world) : IHandler<RecycleObjectCommand>
{
    public Task<CommandResult> Handle(RecycleObjectCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var player = cmd.Player;
        var item = cmd.Target;

        if (!item.IsOwnedBy(player))
        {
            result.Add(player, new SystemMessageEvent("You can only recycle objects you created."));
            return Task.FromResult(result);
        }

        var room = world.GetLocationOrThrow(player);
        var wasInRoom = item.Location is not null;

        // Remove the object from its container (room or player inventory)
        item.Container?.RemoveFromContents(item);

        // Mark the room as modified so the deletion persists
        world.MarkRoomModified(room);

        result.Add(player, new ObjectRecycledEvent(player, item));

        // Broadcast to observers only if the item was in the room
        if (wasInRoom)
        {
            result.BroadcastToAllButPlayer(room, player, new ObjectRecycledEvent(player, item));
        }

        return Task.FromResult(result);
    }
}
