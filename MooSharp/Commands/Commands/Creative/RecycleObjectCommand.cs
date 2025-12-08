using MooSharp.Actors.Players;
using MooSharp.Commands.Commands.Informational;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;
using MooSharp.Commands.Searching;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Commands.Commands.Creative;

public class RecycleObjectCommand : CommandBase<RecycleObjectCommand>
{
    public required Player Player { get; init; }
    public required string Target { get; init; }
}

public class RecycleObjectCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["@recycle"];
    public CommandCategory Category => CommandCategory.General;

    public string Description => "Permanently destroy an object you created. Usage: @recycle <object>.";

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        var target = ctx.GetRemainingText().Trim();

        command = new RecycleObjectCommand
        {
            Player = ctx.Player,
            Target = target
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

public class RecycleObjectHandler(World.World world, TargetResolver resolver) : IHandler<RecycleObjectCommand>
{
    public Task<CommandResult> Handle(RecycleObjectCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var player = cmd.Player;
        var target = cmd.Target;

        if (string.IsNullOrWhiteSpace(target))
        {
            result.Add(player, new SystemMessageEvent("Usage: @recycle <object>."));
            return Task.FromResult(result);
        }

        var room = world.GetLocationOrThrow(player);
        var search = resolver.FindNearbyObject(player, room, target);

        switch (search.Status)
        {
            case SearchStatus.NotFound:
                result.Add(player, new ItemNotFoundEvent(target));
                break;

            case SearchStatus.IndexOutOfRange:
                result.Add(player, new SystemMessageEvent($"You can't see a '{target}' here."));
                break;

            case SearchStatus.Ambiguous:
                result.Add(player, new AmbiguousInputEvent(target, search.Candidates));
                break;

            case SearchStatus.Found:
                var item = search.Match!;

                if (!item.IsOwnedBy(player))
                {
                    result.Add(player, new SystemMessageEvent("You can only recycle objects you created."));
                    break;
                }

                // Remove the object from its container (room or player inventory)
                item.Container?.RemoveFromContents(item);

                // Mark the room as modified so the deletion persists
                world.MarkRoomModified(room);

                result.Add(player, new ObjectRecycledEvent(player, item));

                // Broadcast to observers only if the item was in the room
                if (item.Location is not null)
                {
                    result.BroadcastToAllButPlayer(room, player, new ObjectRecycledEvent(player, item));
                }

                break;
        }

        return Task.FromResult(result);
    }
}
