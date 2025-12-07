using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Commands.Commands.Items;

public class DropCommand : CommandBase<DropCommand>
{
    public required Player Player { get; init; }
    public required Object Target { get; init; }
}

public class DropCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["drop"];
    public CommandCategory Category => CommandCategory.General;
    public string Description => "Drop an item from your inventory. Usage: drop <item>.";

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        command = null;

        var bind = binder.BindInventoryItem(ctx);
        if (!bind.IsSuccess)
        {
            return bind.ErrorMessage;
        }

        command = new DropCommand
        {
            Player = ctx.Player,
            Target = bind.Value!
        };

        return null;
    }
}

public class DropHandler(World.World world) : IHandler<DropCommand>
{
    public Task<CommandResult> Handle(DropCommand cmd, CancellationToken ct = default)
    {
        var result = new CommandResult();
        var room = world.GetLocationOrThrow(cmd.Player);

        cmd.Target.MoveTo(room);

        var dropEvent = new ItemDroppedEvent(cmd.Target, cmd.Player);
        result.Add(cmd.Player, dropEvent);
        result.Broadcast(room.PlayersInRoom, dropEvent, MessageAudience.Observer, cmd.Player);

        return Task.FromResult(result);
    }
}
// Events/Formatters omitted

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
