using MooSharp.Actors;
using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;

namespace MooSharp.Commands.Commands.Items;

public class CloseCommand : CommandBase<CloseCommand>
{
    public required Player Player { get; init; }
    public required IOpenable Target { get; init; }
}

public class CloseCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["close", "shut"];
    public CommandCategory Category => CommandCategory.General;
    public string Description => "Close a container or door. Usage: 'close <object>'.";

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        command = null;

        var bind = binder.BindOpenable(ctx);
        if (!bind.IsSuccess)
        {
            return bind.ErrorMessage;
        }

        command = new CloseCommand
        {
            Player = ctx.Player,
            Target = bind.Value
        };

        return null;
    }
}

public class CloseHandler : IHandler<CloseCommand>
{
    public Task<CommandResult> Handle(CloseCommand cmd, CancellationToken ct = default)
    {
        var result = new CommandResult();
        var target = cmd.Target;

        if (!target.CanBeOpened)
        {
            result.Add(cmd.Player, new SystemMessageEvent("You can't close that."));
            return Task.FromResult(result);
        }

        if (!target.IsOpen)
        {
            result.Add(cmd.Player, new SystemMessageEvent("It is already closed."));
            return Task.FromResult(result);
        }

        target.IsOpen = false;
        result.Add(cmd.Player, new ItemClosedEvent(cmd.Player, target));
        return Task.FromResult(result);
    }
}
// Event/Formatter omitted for brevity (unchanged)

public record ItemClosedEvent(Player Player, IOpenable Object) : IGameEvent;

public class ItemClosedEventFormatter : IGameEventFormatter<ItemClosedEvent>
{
    public string FormatForActor(ItemClosedEvent gameEvent) => $"You close the {gameEvent.Object.Name}.";

    public string FormatForObserver(ItemClosedEvent gameEvent) =>
        $"{gameEvent.Player.Username} closes the {gameEvent.Object.Name}.";
}