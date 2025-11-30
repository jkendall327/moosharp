using MooSharp.Messaging;

namespace MooSharp;

public class InventoryCommand : CommandBase<InventoryCommand>
{
    public required Player Player { get; init; }
}

public class InventoryCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["i", "inv", "inventory"];

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

        result.Add(cmd.Player, new SelfExaminedEvent(cmd.Player, inventory));

        return Task.FromResult(result);
    }
}
