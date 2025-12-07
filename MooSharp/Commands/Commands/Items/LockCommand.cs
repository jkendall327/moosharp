using MooSharp.Actors;
using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;

namespace MooSharp.Commands.Commands.Items;

public class LockCommand : CommandBase<LockCommand>
{
    public required Player Player { get; init; }
    public required ILockable Target { get; init; }
}

public class LockCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["lock"];
    public CommandCategory Category => CommandCategory.General;
    public string Description => "Lock a lockable object using a key. Usage: 'lock <object>'.";

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        command = null;
        var bind = binder.BindLockable(ctx);

        if (!bind.IsSuccess) return bind.ErrorMessage;

        command = new LockCommand
        {
            Player = ctx.Player,
            Target = bind.Value!
        };

        return null;
    }
}

public class UnlockCommand : CommandBase<UnlockCommand>
{
    public required Player Player { get; init; }
    public required ILockable Target { get; init; }
}

public class UnlockCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["unlock"];
    public CommandCategory Category => CommandCategory.General;
    public string Description => "Unlock a locked object using a key. Usage: 'unlock <object>'.";

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        command = null;
        var bind = binder.BindLockable(ctx);

        if (!bind.IsSuccess) return bind.ErrorMessage;

        command = new UnlockCommand
        {
            Player = ctx.Player,
            Target = bind.Value!
        };

        return null;
    }
}

public class LockHandler : IHandler<LockCommand>
{
    public Task<CommandResult> Handle(LockCommand cmd, CancellationToken ct = default)
    {
        var result = new CommandResult();
        var target = cmd.Target;

        if (!target.CanBeLocked)
        {
            result.Add(cmd.Player, new SystemMessageEvent("You can't lock that."));

            return Task.FromResult(result);
        }

        if (target.IsLocked)
        {
            result.Add(cmd.Player, new SystemMessageEvent("It is already locked."));

            return Task.FromResult(result);
        }

        var hasKey = target.KeyId is null || cmd.Player.Inventory.Any(item => item.KeyId == target.KeyId);

        if (!hasKey)
        {
            result.Add(cmd.Player, new SystemMessageEvent("You don't have the right key."));

            return Task.FromResult(result);
        }

        target.IsLocked = true;

        if (target is IOpenable openable)
        {
            openable.IsOpen = false;
        }

        result.Add(cmd.Player, new ItemLockedEvent(cmd.Player, target));

        return Task.FromResult(result);
    }
}

public class UnlockHandler : IHandler<UnlockCommand>
{
    public Task<CommandResult> Handle(UnlockCommand cmd, CancellationToken ct = default)
    {
        var result = new CommandResult();
        var target = cmd.Target;

        if (!target.CanBeLocked)
        {
            result.Add(cmd.Player, new SystemMessageEvent("You can't unlock that."));

            return Task.FromResult(result);
        }

        if (!target.IsLocked)
        {
            result.Add(cmd.Player, new SystemMessageEvent("It is already unlocked."));

            return Task.FromResult(result);
        }

        var hasKey = target.KeyId is null || cmd.Player.Inventory.Any(item => item.KeyId == target.KeyId);

        if (!hasKey)
        {
            result.Add(cmd.Player, new SystemMessageEvent("You don't have the right key."));

            return Task.FromResult(result);
        }

        target.IsLocked = false;
        result.Add(cmd.Player, new ItemUnlockedEvent(cmd.Player, target));

        return Task.FromResult(result);
    }
}

public record ItemLockedEvent(Player Player, ILockable Object) : IGameEvent;

public class ItemLockedEventFormatter : IGameEventFormatter<ItemLockedEvent>
{
    public string FormatForActor(ItemLockedEvent gameEvent) => $"You lock the {gameEvent.Object.Name}.";

    public string FormatForObserver(ItemLockedEvent gameEvent) =>
        $"{gameEvent.Player.Username} locks the {gameEvent.Object.Name}.";
}

public record ItemUnlockedEvent(Player Player, ILockable Object) : IGameEvent;

public class ItemUnlockedEventFormatter : IGameEventFormatter<ItemUnlockedEvent>
{
    public string FormatForActor(ItemUnlockedEvent gameEvent) => $"You unlock the {gameEvent.Object.Name}.";

    public string FormatForObserver(ItemUnlockedEvent gameEvent) =>
        $"{gameEvent.Player.Username} unlocks the {gameEvent.Object.Name}.";
}