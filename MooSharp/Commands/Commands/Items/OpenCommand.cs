using MooSharp.Actors;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Searching;
using MooSharp.Messaging;
using MooSharp.World;
using Object = MooSharp.Actors.Object;

namespace MooSharp.Commands.Commands.Items;

public class OpenCommand : CommandBase<OpenCommand>
{
    public required Player Player { get; init; }
    public required string Target { get; init; }
}

public class OpenCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["open"];
    public CommandCategory Category => CommandCategory.General;

    public string Description => "Open a container. No effect on already-open containers. Usage: 'open <object>'.";

    public ICommand Create(Player player, string args)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(args);

        return new OpenCommand
        {
            Player = player,
            Target = args
        };
    }
}

public record ItemOpenedEvent(Player Player, Object Object) : IGameEvent;

public class ItemOpenedEventEventFormatter : IGameEventFormatter<ItemOpenedEvent>
{
    public string FormatForActor(ItemOpenedEvent gameEvent) => $"You opened the {gameEvent.Object.Name}.";

    public string FormatForObserver(ItemOpenedEvent gameEvent) =>
        $"{gameEvent.Player.Username} opened the {gameEvent.Object.Name}.";
}

public class OpenHandler(World.World world, TargetResolver resolver) : IHandler<OpenCommand>
{
    public Task<CommandResult> Handle(OpenCommand command, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();

        var player = command.Player;

        var currentRoom = world.GetLocationOrThrow(player);

        var searchResult = resolver.FindObjects(currentRoom.Contents, command.Target);

        var target = searchResult.Match;

        if (target is null)
        {
            result.Add(player, new SystemMessageEvent("No item was found to open."));

            return Task.FromResult(result);
        }

        var openable = target.IsOpenable;
        var open = target.IsOpen;

        if (!openable && open)
        {
            throw new InvalidOperationException("Object was open but not openable.");
        }

        if (!openable)
        {
            result.Add(player, new SystemMessageEvent("You can't open that."));

            return Task.FromResult(result);
        }

        if (open)
        {
            result.Add(player, new SystemMessageEvent("But it's already open."));

            return Task.FromResult(result);
        }

        target.IsOpen = true;

        result.Add(player, new ItemOpenedEvent(player, target));

        return Task.FromResult(result);
    }
}