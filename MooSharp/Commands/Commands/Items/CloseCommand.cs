using MooSharp.Actors;
using MooSharp.Commands.Machinery;
using MooSharp.Messaging;
using MooSharp.World;
using Object = MooSharp.Actors.Object;

namespace MooSharp.Commands.Commands.Items;

public class CloseCommand : CommandBase<CloseCommand>
{
    public required Player Player { get; set; }
    public required string Target { get; set; }
}

public class CloseCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["close", "shut"];
    public CommandCategory Category => CommandCategory.General;

    public string Description => "Close a container or door. Usage: 'close <object>'.";

    public ICommand Create(Player player, string args)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(args);

        return new CloseCommand
        {
            Player = player,
            Target = args
        };
    }
}

public record ItemClosedEvent(Player Player, Object Object) : IGameEvent;

public class ItemClosedEventFormatter : IGameEventFormatter<ItemClosedEvent>
{
    public string FormatForActor(ItemClosedEvent gameEvent) => $"You close the {gameEvent.Object.Name}.";

    public string FormatForObserver(ItemClosedEvent gameEvent) =>
        $"{gameEvent.Player.Username} closes the {gameEvent.Object.Name}.";
}

public class CloseHandler(World.World world, Searching.TargetResolver resolver) : IHandler<CloseCommand>
{
    public Task<CommandResult> Handle(CloseCommand command, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();

        var player = command.Player;

        var currentRoom = world.GetLocationOrThrow(player);

        var searchResult = resolver.FindObjects(currentRoom.Contents, command.Target);

        var target = searchResult.Match;

        if (target is null)
        {
            result.Add(player, new SystemMessageEvent("No item was found to close."));

            return Task.FromResult(result);
        }

        var openable = target.IsOpenable;
        var open = target.IsOpen;

        if (!openable)
        {
            result.Add(player, new SystemMessageEvent("You can't close that."));

            return Task.FromResult(result);
        }

        if (!open)
        {
            result.Add(player, new SystemMessageEvent("It is already closed."));

            return Task.FromResult(result);
        }

        target.IsOpen = false;

        result.Add(player, new ItemClosedEvent(player, target));

        return Task.FromResult(result);
    }
}