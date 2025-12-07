using MooSharp.Actors.Players;
using MooSharp.Commands.Commands.Informational;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Presentation;
using MooSharp.Commands.Searching;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Commands.Commands.Items;

public class DropCommand : CommandBase<DropCommand>
{
    public required Player Player { get; init; }
    public required string Target { get; init; }
}

public class DropCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["drop"];
    public CommandCategory Category => CommandCategory.General;

    public string Description => "Drop an item from your inventory. Usage: drop <item>.";

    public ICommand Create(Player player, string args) =>
        new DropCommand
        {
            Player = player,
            Target = args
        };
}

public class DropHandler(World.World world, TargetResolver resolver) : IHandler<DropCommand>
{
    public Task<CommandResult> Handle(DropCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var player = cmd.Player;
        var target = cmd.Target.Trim();

        if (string.IsNullOrWhiteSpace(target))
        {
            result.Add(player, new SystemMessageEvent("Drop what?"));
            return Task.FromResult(result);
        }

        var room = world.GetLocationOrThrow(player);

        var search = resolver.FindObjects(player.Inventory, target);

        switch (search.Status)
        {
            case SearchStatus.NotFound:
                result.Add(player, new ItemNotCarriedEvent(target));
                break;

            case SearchStatus.IndexOutOfRange:
                result.Add(player, new SystemMessageEvent($"You don't have a '{target}'."));
                break;

            case SearchStatus.Ambiguous:
                result.Add(player, new AmbiguousInputEvent(target, search.Candidates));
                break;

            case SearchStatus.Found:
                var item = search.Match!;
                item.MoveTo(room);

                var dropEvent = new ItemDroppedEvent(item, player);
                result.Add(player, dropEvent);
                result.Broadcast(room.PlayersInRoom, dropEvent, MessageAudience.Observer, player);
                break;
        }

        return Task.FromResult(result);
    }
}

public record ItemNotCarriedEvent(string ItemName) : IGameEvent;

public class ItemNotCarriedEventFormatter : IGameEventFormatter<ItemNotCarriedEvent>
{
    public string FormatForActor(ItemNotCarriedEvent gameEvent) => $"You aren't carrying a {gameEvent.ItemName}.";

    public string FormatForObserver(ItemNotCarriedEvent gameEvent) => FormatForActor(gameEvent);
}

public record ItemDroppedEvent(Object Item, Player Player) : IGameEvent;

public class ItemDroppedEventFormatter : IGameEventFormatter<ItemDroppedEvent>
{
    public string FormatForActor(ItemDroppedEvent gameEvent) => $"You drop the {gameEvent.Item.Name}.";

    public string FormatForObserver(ItemDroppedEvent gameEvent) =>
        $"{gameEvent.Player.Username} drops the {gameEvent.Item.Name}.";
}
