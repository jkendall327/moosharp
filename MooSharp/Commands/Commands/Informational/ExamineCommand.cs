using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MooSharp.Actors.Players;
using MooSharp.Actors.Rooms;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Commands.Commands.Informational;

public class ExamineRoomCommand : CommandBase<ExamineRoomCommand>
{
    public required Player Player { get; init; }
    public required Room Room { get; init; }
}

public class ExamineSelfCommand : CommandBase<ExamineSelfCommand>
{
    public required Player Player { get; init; }
}

public class ExaminePlayerCommand : CommandBase<ExaminePlayerCommand>
{
    public required Player Player { get; init; }
    public required Player Target { get; init; }
}

public class ExamineObjectCommand : CommandBase<ExamineObjectCommand>
{
    public required Player Player { get; init; }
    public required Object Target { get; init; }
}

public class ExamineExitCommand : CommandBase<ExamineExitCommand>
{
    public required Player Player { get; init; }
    public required Exit Target { get; init; }
}

public class ExamineAmbiguousObjectCommand : CommandBase<ExamineAmbiguousObjectCommand>
{
    public required Player Player { get; init; }
    public required string Input { get; init; }
    public required IReadOnlyCollection<Object> Candidates { get; init; }
}

public class ExamineAmbiguousExitCommand : CommandBase<ExamineAmbiguousExitCommand>
{
    public required Player Player { get; init; }
    public required string Input { get; init; }
    public required IReadOnlyCollection<Exit> Candidates { get; init; }
}

public class ExamineItemNotFoundCommand : CommandBase<ExamineItemNotFoundCommand>
{
    public required Player Player { get; init; }
    public required string TargetName { get; init; }
}

public class ExamineSystemMessageCommand : CommandBase<ExamineSystemMessageCommand>
{
    public required Player Player { get; init; }
    public required string Message { get; init; }
}

public class ExamineCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["examine", "ex", "x", "view", "look"];

    public string Description => "Inspect yourself, an item, or the room. Usage: examine <target>.";
    public CommandCategory Category => CommandCategory.General;

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        var resolutionResult = binder.BindExamineTarget(ctx);

        if (!resolutionResult.IsSuccess)
        {
            command = null;
            return resolutionResult.ErrorMessage;
        }

        var resolution = resolutionResult.Value;

        command = resolution.Kind switch
        {
            ExamineResolutionKind.Room => new ExamineRoomCommand
            {
                Player = ctx.Player,
                Room = ctx.Room
            },

            ExamineResolutionKind.Self => new ExamineSelfCommand
            {
                Player = ctx.Player
            },

            ExamineResolutionKind.Player => new ExaminePlayerCommand
            {
                Player = ctx.Player,
                Target = resolution.PlayerTarget!
            },

            ExamineResolutionKind.Object => new ExamineObjectCommand
            {
                Player = ctx.Player,
                Target = resolution.ObjectTarget!
            },

            ExamineResolutionKind.Exit => new ExamineExitCommand
            {
                Player = ctx.Player,
                Target = resolution.ExitTarget!
            },

            ExamineResolutionKind.AmbiguousObject => new ExamineAmbiguousObjectCommand
            {
                Player = ctx.Player,
                Input = resolution.TargetText,
                Candidates = resolution.ObjectCandidates
            },

            ExamineResolutionKind.AmbiguousExit => new ExamineAmbiguousExitCommand
            {
                Player = ctx.Player,
                Input = resolution.TargetText,
                Candidates = resolution.ExitCandidates
            },

            ExamineResolutionKind.ObjectIndexOutOfRange => new ExamineSystemMessageCommand
            {
                Player = ctx.Player,
                Message = $"You can't see a '{resolution.TargetText}' here."
            },

            ExamineResolutionKind.ExitIndexOutOfRange => new ExamineSystemMessageCommand
            {
                Player = ctx.Player,
                Message = $"You don't see that many '{resolution.TargetText}' exits."
            },

            ExamineResolutionKind.ItemNotFound => new ExamineItemNotFoundCommand
            {
                Player = ctx.Player,
                TargetName = resolution.TargetText
            },

            _ => null
        };

        return command is null ? "Unable to parse examine target." : null;
    }
}

public class ExamineRoomHandler : IHandler<ExamineRoomCommand>
{
    public Task<CommandResult> Handle(ExamineRoomCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var description = cmd.Room.DescribeFor(cmd.Player, useLongDescription: true);

        result.Add(cmd.Player, new RoomDescriptionEvent(description));

        return Task.FromResult(result);
    }
}

public class ExamineSelfHandler : IHandler<ExamineSelfCommand>
{
    public Task<CommandResult> Handle(ExamineSelfCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var inventory = cmd.Player.Inventory.ToList();

        result.Add(cmd.Player, new SelfExaminedEvent(cmd.Player, inventory));

        return Task.FromResult(result);
    }
}

public class ExaminePlayerHandler : IHandler<ExaminePlayerCommand>
{
    public Task<CommandResult> Handle(ExaminePlayerCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var activityState = PlayerActivityHelper.GetActivityState(cmd.Target, DateTime.UtcNow);

        result.Add(cmd.Player, new PlayerExaminedEvent(cmd.Player, cmd.Target, activityState));

        return Task.FromResult(result);
    }
}

public class ExamineObjectHandler : IHandler<ExamineObjectCommand>
{
    public Task<CommandResult> Handle(ExamineObjectCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();

        result.Add(cmd.Player, new ObjectExaminedEvent(cmd.Target));

        return Task.FromResult(result);
    }
}

public class ExamineExitHandler : IHandler<ExamineExitCommand>
{
    public Task<CommandResult> Handle(ExamineExitCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();

        result.Add(cmd.Player, new ExitExaminedEvent(cmd.Target));

        return Task.FromResult(result);
    }
}

public class ExamineAmbiguousObjectHandler : IHandler<ExamineAmbiguousObjectCommand>
{
    public Task<CommandResult> Handle(ExamineAmbiguousObjectCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();

        result.Add(cmd.Player, new AmbiguousInputEvent(cmd.Input, cmd.Candidates));

        return Task.FromResult(result);
    }
}

public class ExamineAmbiguousExitHandler : IHandler<ExamineAmbiguousExitCommand>
{
    public Task<CommandResult> Handle(ExamineAmbiguousExitCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();

        result.Add(cmd.Player, new AmbiguousExitEvent(cmd.Input, cmd.Candidates));

        return Task.FromResult(result);
    }
}

public class ExamineItemNotFoundHandler : IHandler<ExamineItemNotFoundCommand>
{
    public Task<CommandResult> Handle(ExamineItemNotFoundCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();

        result.Add(cmd.Player, new ItemNotFoundEvent(cmd.TargetName));

        return Task.FromResult(result);
    }
}

public class ExamineSystemMessageHandler : IHandler<ExamineSystemMessageCommand>
{
    public Task<CommandResult> Handle(ExamineSystemMessageCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();

        result.Add(cmd.Player, new SystemMessageEvent(cmd.Message));

        return Task.FromResult(result);
    }
}

public record SelfExaminedEvent(Player Player, IReadOnlyCollection<Object> Inventory) : IGameEvent;

public class SelfExaminedEventFormatter : IGameEventFormatter<SelfExaminedEvent>
{
    public string FormatForActor(SelfExaminedEvent gameEvent)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You took a look at yourself. You're looking pretty good.");

        if (gameEvent.Inventory.Count > 0)
        {
            sb.AppendLine("You have:");

            foreach (var item in gameEvent.Inventory)
            {
                var valueText = item.Value != 0 ? $" ({item.Value:F2})" : "";
                sb.AppendLine($"{item.DescribeWithState()}{valueText}");
            }

            var totalValue = gameEvent.Inventory.Sum(i => i.Value);
            sb.AppendLine($"Total value: {totalValue:F2}");
        }

        return sb.ToString().TrimEnd();
    }

    public string FormatForObserver(SelfExaminedEvent gameEvent) => "Someone seems to be checking themselves out.";
}

public record PlayerExaminedEvent(Player Viewer, Player Target, PlayerActivityState Activity) : IGameEvent;

public class PlayerExaminedEventFormatter : IGameEventFormatter<PlayerExaminedEvent>
{
    public string FormatForActor(PlayerExaminedEvent gameEvent)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"{gameEvent.Target.Username} is here.");
        sb.Append($"They seem to be {PlayerActivityHelper.FormatStatusLabel(gameEvent.Activity)}.");

        return sb.ToString().TrimEnd();
    }

    public string? FormatForObserver(PlayerExaminedEvent gameEvent)
        => $"{gameEvent.Viewer.Username} looks closely at {gameEvent.Target.Username}.";
}

public record ObjectExaminedEvent(Object Item) : IGameEvent;

public class ObjectExaminedEventFormatter : IGameEventFormatter<ObjectExaminedEvent>
{
    public string FormatForActor(ObjectExaminedEvent gameEvent)
        => FormatDescription(gameEvent.Item);

    public string FormatForObserver(ObjectExaminedEvent gameEvent)
        => FormatDescription(gameEvent.Item);

    private static string FormatDescription(Object item)
    {
        var sb = new StringBuilder();

        sb.AppendLine(item.DescribeWithState());

        if (!string.IsNullOrWhiteSpace(item.TextContent))
        {
            sb.AppendLine("There is something written on it.");
        }

        if (item.Value != 0)
        {
            sb.AppendLine($"Value: {item.Value:F2}");
        }

        return sb.ToString().TrimEnd();
    }
}

public record ExitExaminedEvent(Exit Exit) : IGameEvent;

public class ExitExaminedEventFormatter : IGameEventFormatter<ExitExaminedEvent>
{
    public string FormatForActor(ExitExaminedEvent gameEvent) => gameEvent.Exit.Description;

    public string FormatForObserver(ExitExaminedEvent gameEvent) => gameEvent.Exit.Description;
}

public record AmbiguousInputEvent(string Input, IReadOnlyCollection<Object> Candidates) : IGameEvent;

public class AmbiguousInputEventFormatter : IGameEventFormatter<AmbiguousInputEvent>
{
    public string FormatForActor(AmbiguousInputEvent evt)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Which '{evt.Input}' do you mean?");

        var i = 1;
        foreach (var candidate in evt.Candidates)
        {
            sb.AppendLine($"{i++}. {candidate.Name}");
        }

        sb.Append("Type the name and the number (e.g., 'sword 2').");
        return sb.ToString();
    }

    public string? FormatForObserver(AmbiguousInputEvent evt) => null;
}

public record AmbiguousExitEvent(string Input, IReadOnlyCollection<Exit> Candidates) : IGameEvent;

public class AmbiguousExitEventFormatter : IGameEventFormatter<AmbiguousExitEvent>
{
    public string FormatForActor(AmbiguousExitEvent evt)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Which '{evt.Input}' do you mean?");

        var i = 1;
        foreach (var candidate in evt.Candidates)
        {
            sb.AppendLine($"{i++}. {candidate.Name}");
        }

        sb.Append("Type the name and the number (e.g., 'north 2').");
        return sb.ToString();
    }

    public string? FormatForObserver(AmbiguousExitEvent evt) => null;
}

public record ItemNotFoundEvent(string ItemName) : IGameEvent;

public class ItemNotFoundEventFormatter : IGameEventFormatter<ItemNotFoundEvent>
{
    public string FormatForActor(ItemNotFoundEvent gameEvent) => $"There is no {gameEvent.ItemName} here.";

    public string FormatForObserver(ItemNotFoundEvent gameEvent) => FormatForActor(gameEvent);
}
