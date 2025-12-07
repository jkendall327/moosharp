using MooSharp.Actors.Players;
using MooSharp.Commands.Commands.Informational;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;
using MooSharp.Commands.Searching;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Commands.Commands.Items;

public class GiveCommand : CommandBase<GiveCommand>
{
    public required Player Player { get; init; }
    public required Player Target { get; init; }
    public required Object Item { get; init; }
}

public class GiveCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["give"];
    public CommandCategory Category => CommandCategory.General;

    public string Description => "Give an item to another player. Usage: give <target> to <item>.";

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        command = null;

        // Try to get the item from inventory
        var itemResult = binder.BindInventoryItem(ctx);

        if (!itemResult.IsSuccess)
        {
            return itemResult.ErrorMessage;
        }

        // Optionally consume "to"
        binder.ConsumePreposition(ctx, "to");

        var playerResult = binder.BindPlayerInRoom(ctx);

        if (!playerResult.IsSuccess)
        {
            return playerResult.ErrorMessage;
        }

        command = new GiveCommand
        {
            Player = ctx.Player,
            Item = itemResult.Value!,
            Target = playerResult.Value!
        };

        return null;
    }
}

public class GiveHandler(World.World world) : IHandler<GiveCommand>
{
    public Task<CommandResult> Handle(GiveCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var player = cmd.Player;

        var room = world.GetLocationOrThrow(player);

        var item = cmd.Item;
        var recipient = cmd.Target;

        item.MoveTo(recipient);

        var giveEvent = new ItemGivenEvent(player, recipient, item);
        var receiveEvent = new ItemReceivedEvent(player, recipient, item);

        result.Add(player, giveEvent);
        result.Add(recipient, receiveEvent);
        result.Broadcast(room.PlayersInRoom, giveEvent, MessageAudience.Observer, player, recipient);

        return Task.FromResult(result);
    }
}

public record ItemGivenEvent(Player Sender, Player Recipient, Object Item) : IGameEvent;

public class ItemGivenEventFormatter : IGameEventFormatter<ItemGivenEvent>
{
    public string FormatForActor(ItemGivenEvent gameEvent) =>
        $"You give the {gameEvent.Item.Name} to {gameEvent.Recipient.Username}.";

    public string FormatForObserver(ItemGivenEvent gameEvent) =>
        $"{gameEvent.Sender.Username} gives the {gameEvent.Item.Name} to {gameEvent.Recipient.Username}.";
}

public record ItemReceivedEvent(Player Sender, Player Recipient, Object Item) : IGameEvent;

public class ItemReceivedEventFormatter : IGameEventFormatter<ItemReceivedEvent>
{
    public string FormatForActor(ItemReceivedEvent gameEvent) =>
        $"{gameEvent.Sender.Username} gives you the {gameEvent.Item.Name}.";

    public string FormatForObserver(ItemReceivedEvent gameEvent) =>
        $"{gameEvent.Sender.Username} gives the {gameEvent.Item.Name} to {gameEvent.Recipient.Username}.";
}