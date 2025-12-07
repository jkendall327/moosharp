using MooSharp.Actors.Objects;
using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Commands.Commands.Creative;

public class WriteCommand : CommandBase<WriteCommand>
{
    public required Player Player { get; init; }
    public required Object Target { get; init; }
    public required string Text { get; init; }
}

public class WriteCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["write"];
    public CommandCategory Category => CommandCategory.General;
    public string Description => "Write a message on an item. Usage: write [on] <item> <text>.";

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        command = null;

        // Optionally consume "on". e.g. "Write on board" vs "Write board"
        binder.ConsumePreposition(ctx, "on");

        var itemResult = binder.BindNearbyObject(ctx);
        if (!itemResult.IsSuccess)
        {
            return itemResult.ErrorMessage;
        }

        var text = ctx.GetRemainingText();
        if (string.IsNullOrWhiteSpace(text))
        {
            return "Write what?";
        }

        command = new WriteCommand
        {
            Player = ctx.Player,
            Target = itemResult.Value!,
            Text = text
        };

        return null;
    }
}

public class WriteHandler(World.World world) : IHandler<WriteCommand>
{
    public Task<CommandResult> Handle(WriteCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var item = cmd.Target;

        // State Check: Is the object physically capable of being written on?
        if (!item.Flags.HasFlag(ObjectFlags.Writeable))
        {
            result.Add(cmd.Player, new SystemMessageEvent("You can't write on that."));
            return Task.FromResult(result);
        }

        // Execute
        item.WriteText(cmd.Text);

        var writeEvent = new ObjectWrittenOnEvent(cmd.Player, item, cmd.Text);
        result.Add(cmd.Player, writeEvent);

        // Logic: If the item is in the room (public), show everyone. 
        // If it's in the player's inventory (private), only show the player.
        var currentRoom = world.GetLocationOrThrow(cmd.Player);

        if (item.Location is { } location && ReferenceEquals(location, currentRoom))
        {
            result.BroadcastToAllButPlayer(currentRoom, cmd.Player, writeEvent);
        }

        return Task.FromResult(result);
    }
}

public record ObjectWrittenOnEvent(Player Player, Object Item, string Text) : IGameEvent;

public class ObjectWrittenOnEventFormatter : IGameEventFormatter<ObjectWrittenOnEvent>
{
    public string FormatForActor(ObjectWrittenOnEvent gameEvent) =>
        $"You write \"{gameEvent.Text}\" on the {gameEvent.Item.Name}.";

    public string FormatForObserver(ObjectWrittenOnEvent gameEvent) =>
        $"{gameEvent.Player.Username} writes \"{gameEvent.Text}\" on the {gameEvent.Item.Name}.";
}