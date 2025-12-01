using MooSharp.Actors;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Searching;
using MooSharp.Messaging;
using MooSharp.World;
using Object = MooSharp.Actors.Object;

namespace MooSharp.Commands.Commands.Items;

public class LockCommand : CommandBase<LockCommand>
{
    public required Player Player { get; init; }
    public required string Target { get; init; }
}

public class LockCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["lock"];
    public CommandCategory Category => CommandCategory.General;

    public string Description => "Lock a lockable object using a key. Usage: 'lock <object>'.";

    public ICommand Create(Player player, string args)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(args);

        return new LockCommand
        {
            Player = player,
            Target = args
        };
    }
}

public class UnlockCommand : CommandBase<UnlockCommand>
{
    public required Player Player { get; init; }
    public required string Target { get; init; }
}

public class UnlockCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["unlock"];
    public CommandCategory Category => CommandCategory.General;

    public string Description => "Unlock a locked object using a key. Usage: 'unlock <object>'.";

    public ICommand Create(Player player, string args)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(args);

        return new UnlockCommand
        {
            Player = player,
            Target = args
        };
    }
}

public record ItemLockedEvent(Player Player, Object Object) : IGameEvent;

public class ItemLockedEventFormatter : IGameEventFormatter<ItemLockedEvent>
{
    public string FormatForActor(ItemLockedEvent gameEvent) => $"You lock the {gameEvent.Object.Name}.";

    public string FormatForObserver(ItemLockedEvent gameEvent) =>
        $"{gameEvent.Player.Username} locks the {gameEvent.Object.Name}.";
}

public record ItemUnlockedEvent(Player Player, Object Object) : IGameEvent;

public class ItemUnlockedEventFormatter : IGameEventFormatter<ItemUnlockedEvent>
{
    public string FormatForActor(ItemUnlockedEvent gameEvent) => $"You unlock the {gameEvent.Object.Name}.";

    public string FormatForObserver(ItemUnlockedEvent gameEvent) =>
        $"{gameEvent.Player.Username} unlocks the {gameEvent.Object.Name}.";
}

public class LockHandler(World.World world, TargetResolver resolver) : IHandler<LockCommand>
{
    public Task<CommandResult> Handle(LockCommand command, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var player = command.Player;

        var room = world.GetLocationOrThrow(player);
        var searchResult = resolver.FindObjects(room.Contents, command.Target);
        var target = searchResult.Match;

        if (target is null)
        {
            result.Add(player, new SystemMessageEvent("No item was found to lock."));

            return Task.FromResult(result);
        }

        if (!target.IsLockable)
        {
            result.Add(player, new SystemMessageEvent("You can't lock that."));

            return Task.FromResult(result);
        }

        if (target.IsLocked)
        {
            result.Add(player, new SystemMessageEvent("It is already locked."));

            return Task.FromResult(result);
        }

        var hasKey = target.KeyId is null || player.Inventory.Any(item => item.KeyId == target.KeyId);

        if (!hasKey)
        {
            result.Add(player, new SystemMessageEvent("You don't have the right key."));

            return Task.FromResult(result);
        }

        target.IsLocked = true;

        result.Add(player, new ItemLockedEvent(player, target));

        return Task.FromResult(result);
    }
}

public class UnlockHandler(World.World world, TargetResolver resolver) : IHandler<UnlockCommand>
{
    public Task<CommandResult> Handle(UnlockCommand command, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var player = command.Player;

        var room = world.GetLocationOrThrow(player);
        var searchResult = resolver.FindObjects(room.Contents, command.Target);
        var target = searchResult.Match;

        if (target is null)
        {
            result.Add(player, new SystemMessageEvent("No item was found to unlock."));

            return Task.FromResult(result);
        }

        if (!target.IsLockable)
        {
            result.Add(player, new SystemMessageEvent("You can't unlock that."));

            return Task.FromResult(result);
        }

        if (!target.IsLocked)
        {
            result.Add(player, new SystemMessageEvent("It is already unlocked."));

            return Task.FromResult(result);
        }

        var hasKey = target.KeyId is null || player.Inventory.Any(item => item.KeyId == target.KeyId);

        if (!hasKey)
        {
            result.Add(player, new SystemMessageEvent("You don't have the right key."));

            return Task.FromResult(result);
        }

        target.IsLocked = false;

        result.Add(player, new ItemUnlockedEvent(player, target));

        return Task.FromResult(result);
    }
}
