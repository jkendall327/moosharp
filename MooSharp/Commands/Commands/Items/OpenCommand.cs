using MooSharp.Actors;
using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;

namespace MooSharp.Commands.Commands.Items;

public class OpenCommand : CommandBase<OpenCommand>
{
    public required Player Player { get; init; }
    public required IOpenable Target { get; init; }
}

public class OpenCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["open"];
    public CommandCategory Category => CommandCategory.General;
    public string Description => "Open a container. Usage: 'open <object>'.";

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        command = null;
        var bind = binder.BindOpenable(ctx);

        if (!bind.IsSuccess)
        {
            return bind.ErrorMessage;
        }

        command = new OpenCommand
        {
            Player = ctx.Player,
            Target = bind.Value!
        };

        return null;
    }
}

public class OpenHandler : IHandler<OpenCommand>
{
    public Task<CommandResult> Handle(OpenCommand cmd, CancellationToken ct = default)
    {
        var result = new CommandResult();
        var target = cmd.Target;

        if (!target.CanBeOpened)
        {
            result.Add(cmd.Player, new SystemMessageEvent("You can't open that."));

            return Task.FromResult(result);
        }

        if (target is ILockable {IsLocked: true})
        {
            result.Add(cmd.Player, new SystemMessageEvent("It's locked."));

            return Task.FromResult(result);
        }

        if (target.IsOpen)
        {
            result.Add(cmd.Player, new SystemMessageEvent("It's already open."));

            return Task.FromResult(result);
        }

        target.IsOpen = true;
        result.Add(cmd.Player, new ItemOpenedEvent(cmd.Player, target));

        return Task.FromResult(result);
    }
}

public record ItemOpenedEvent(Player Player, IOpenable Object) : IGameEvent;

public class ItemOpenedEventEventFormatter : IGameEventFormatter<ItemOpenedEvent>
{
    public string FormatForActor(ItemOpenedEvent gameEvent) => $"You opened the {gameEvent.Object.Name}.";

    public string FormatForObserver(ItemOpenedEvent gameEvent) =>
        $"{gameEvent.Player.Username} opened the {gameEvent.Object.Name}.";
}