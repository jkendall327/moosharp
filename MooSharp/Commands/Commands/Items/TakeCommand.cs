using MooSharp.Actors;
using MooSharp.Actors.Players;
using MooSharp.Actors.Rooms;
using MooSharp.Commands.Commands.Informational;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Presentation;
using MooSharp.Commands.Searching;
using MooSharp.World;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Commands.Commands.Items;

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

    public string Description => "Pick up an item from the room. Usage: take <item>.";

    public CommandCategory Category => CommandCategory.General;

    public ICommand Create(Player player, string args) =>
        new TakeCommand
        {
            Player = player,
            Target = args
        };
}

public class TakeHandler(World.World world, TargetResolver resolver) : IHandler<TakeCommand>
{
    public Task<CommandResult> Handle(TakeCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var player = cmd.Player;

        var ownedItem = player.Inventory.FirstOrDefault(o => MatchesTarget(o, cmd.Target));

        if (ownedItem is not null)
        {
            result.Add(player, new ItemAlreadyInPossessionEvent(ownedItem));

            return Task.FromResult(result);
        }

        var currentLocation = world.GetLocationOrThrow(player);

        var search = resolver.FindObjects(currentLocation.Contents, cmd.Target);

        switch (search.Status)
        {
            case SearchStatus.NotFound:
                var otherOwned = FindOtherOwnedItem(currentLocation, cmd.Target, player);

                if (otherOwned is not null)
                {
                    result.Add(player, new ItemOwnedByOtherEvent(otherOwned.Value.Item, otherOwned.Value.Owner));
                }
                else
                {
                    result.Add(player, new ItemNotFoundEvent(cmd.Target));
                }

                break;

            case SearchStatus.IndexOutOfRange:
                result.Add(player, new SystemMessageEvent($"You can't see a '{cmd.Target}' here."));

                break;

            case SearchStatus.Ambiguous:
                result.Add(player, new AmbiguousInputEvent(cmd.Target, search.Candidates));

                break;

            case SearchStatus.Found:
                var o = search.Match!;

                if (o.IsScenery)
                {
                    result.Add(player, new SystemMessageEvent("You can't pick that up."));

                    return Task.FromResult(result);
                }

                if (o.Owner is null)
                {
                    o.MoveTo(player);
                    result.Add(player, new ItemTakenEvent(o));
                }
                else if (o.Owner == player)
                {
                    result.Add(player, new ItemAlreadyInPossessionEvent(o));
                }
                else
                {
                    result.Add(player, new ItemOwnedByOtherEvent(o, o.Owner));
                }

                break;
        }

        return Task.FromResult(result);
    }

    private static bool MatchesTarget(Object item, string target)
    {
        return item.Name.Equals(target, StringComparison.OrdinalIgnoreCase) ||
               item.Keywords.Contains(target, StringComparer.OrdinalIgnoreCase) ||
               item.Name.Contains(target, StringComparison.OrdinalIgnoreCase);
    }

    private static (Object Item, Player Owner)? FindOtherOwnedItem(Room room, string target, Player actor)
    {
        foreach (var occupant in room.PlayersInRoom.Where(p => p != actor))
        {
            var match = occupant.Inventory.FirstOrDefault(item => MatchesTarget(item, target));

            if (match is not null)
            {
                return (match, occupant);
            }
        }

        return null;
    }
}

public record ItemTakenEvent(Object Item) : IGameEvent;

public class ItemTakenEventFormatter : IGameEventFormatter<ItemTakenEvent>
{
    public string FormatForActor(ItemTakenEvent gameEvent) => $"You take the {gameEvent.Item.Name}.";

    public string FormatForObserver(ItemTakenEvent gameEvent) => $"Someone takes the {gameEvent.Item.Name}.";
}

public record ItemAlreadyInPossessionEvent(Object Item) : IGameEvent;

public class ItemAlreadyInPossessionEventFormatter : IGameEventFormatter<ItemAlreadyInPossessionEvent>
{
    public string FormatForActor(ItemAlreadyInPossessionEvent gameEvent) =>
        $"You already have the {gameEvent.Item.Name}.";

    public string FormatForObserver(ItemAlreadyInPossessionEvent gameEvent) =>
        $"Someone already has the {gameEvent.Item.Name}.";
}

public record ItemOwnedByOtherEvent(Object Item, Player Owner) : IGameEvent;

public class ItemOwnedByOtherEventFormatter : IGameEventFormatter<ItemOwnedByOtherEvent>
{
    public string FormatForActor(ItemOwnedByOtherEvent gameEvent) =>
        $"{gameEvent.Owner.Username} already has the {gameEvent.Item.Name}!";

    public string FormatForObserver(ItemOwnedByOtherEvent gameEvent) =>
        $"{gameEvent.Owner.Username} already has the {gameEvent.Item.Name}!";
}