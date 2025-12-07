using System.Text;
using MooSharp.Actors;
using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Presentation;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Commands.Commands.Informational;

public class InventoryCommand : CommandBase<InventoryCommand>
{
    public required Player Player { get; init; }
}

public class InventoryCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["i", "inv", "inventory"];
    public CommandCategory Category => CommandCategory.General;

    public string Description => "Check what you're carrying.";

    public ICommand Create(Player player, string args) =>
        new InventoryCommand
        {
            Player = player
        };
}

public class InventoryHandler : IHandler<InventoryCommand>
{
    public Task<CommandResult> Handle(InventoryCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var inventory = cmd.Player.Inventory.ToList();

        result.Add(cmd.Player, new InventoryExaminedEvent(cmd.Player, inventory));

        return Task.FromResult(result);
    }
}

public record InventoryExaminedEvent(Player Player, IReadOnlyCollection<Object> Inventory) : IGameEvent;

public class InventoryExaminedEventFormatter : IGameEventFormatter<InventoryExaminedEvent>
{
    public string FormatForActor(InventoryExaminedEvent gameEvent)
    {
        if (gameEvent.Inventory.Count == 0)
        {
            return "You aren't carrying anything.";
        }

        var sb = new StringBuilder();

        sb.AppendLine("You are carrying:");

        foreach (var item in gameEvent.Inventory)
        {
            var valueText = item.Value != 0 ? $" ({item.Value:F2})" : "";
            sb.AppendLine($"{item.DescribeWithState()}{valueText}");
        }

        var totalValue = gameEvent.Inventory.Sum(i => i.Value);

        if (totalValue != 0)
        {
            sb.AppendLine($"Total value: {totalValue:F2}");
        }

        return sb.ToString().TrimEnd();
    }

    public string FormatForObserver(InventoryExaminedEvent gameEvent) => "Someone checks what they're carrying.";
}
