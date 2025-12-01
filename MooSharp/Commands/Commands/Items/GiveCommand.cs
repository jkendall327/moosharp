using MooSharp.Actors;
using MooSharp.Commands.Commands.Informational;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Searching;
using MooSharp.Messaging;
using MooSharp.World;
using Object = MooSharp.Actors.Object;

namespace MooSharp.Commands.Commands.Items;

public class GiveCommand : CommandBase<GiveCommand>
{
    public required Player Player { get; init; }
    public required string Target { get; init; }
    public required string ItemName { get; init; }
}

public class GiveCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["give"];
    public CommandCategory Category => CommandCategory.General;

    public string Description => "Give an item to another player in the same room. Usage: give <target> <item>.";

    public ICommand Create(Player player, string args)
    {
        var split = args.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var target = split.ElementAtOrDefault(0) ?? string.Empty;
        var item = split.ElementAtOrDefault(1) ?? string.Empty;

        return new GiveCommand
        {
            Player = player,
            Target = target,
            ItemName = item
        };
    }
}

public class GiveHandler(World.World world, TargetResolver resolver) : IHandler<GiveCommand>
{
    public Task<CommandResult> Handle(GiveCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var player = cmd.Player;

        if (string.IsNullOrWhiteSpace(cmd.Target) || string.IsNullOrWhiteSpace(cmd.ItemName))
        {
            result.Add(player, new SystemMessageEvent("Usage: give <target> <item>."));
            return Task.FromResult(result);
        }

        var room = world.GetLocationOrThrow(player);

        var recipient = Enumerable
            .FirstOrDefault<Player>(room.PlayersInRoom, p => p.Username.Equals(cmd.Target, StringComparison.OrdinalIgnoreCase));

        if (recipient is null)
        {
            result.Add(player, new SystemMessageEvent($"{cmd.Target} isn't here."));
            return Task.FromResult(result);
        }

        var search = resolver.FindObjects(player.Inventory, cmd.ItemName);

        switch (search.Status)
        {
            case SearchStatus.NotFound:
                result.Add(player, new ItemNotCarriedEvent(cmd.ItemName));
                break;

            case SearchStatus.IndexOutOfRange:
                result.Add(player, new SystemMessageEvent($"You don't have a '{cmd.ItemName}'."));
                break;

            case SearchStatus.Ambiguous:
                result.Add(player, new AmbiguousInputEvent(cmd.ItemName, search.Candidates));
                break;

            case SearchStatus.Found:
                var item = search.Match!;
                item.MoveTo(recipient);

                var giveEvent = new ItemGivenEvent(player, recipient, item);
                var receiveEvent = new ItemReceivedEvent(player, recipient, item);

                result.Add(player, giveEvent);
                result.Add(recipient, receiveEvent);
                result.Broadcast(room.PlayersInRoom, giveEvent, MessageAudience.Observer, player, recipient);
                break;
        }

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
