using MooSharp.Actors;
using MooSharp.Commands.Commands.Informational;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Searching;
using MooSharp.Messaging;
using MooSharp.World;
using Object = MooSharp.Actors.Object;

namespace MooSharp.Commands.Commands.Creative;

public class WriteCommand : CommandBase<WriteCommand>
{
    public required Player Player { get; init; }
    public required string Target { get; init; }
    public required string Text { get; init; }
}

public class WriteCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["write"];
    public CommandCategory Category => CommandCategory.General;

    public string Description => "Write a message on an item. Usage: write on <item> <text>.";

    public ICommand Create(Player player, string args)
    {
        var trimmedArgs = args.Trim();

        if (trimmedArgs.StartsWith("on ", StringComparison.OrdinalIgnoreCase))
        {
            trimmedArgs = trimmedArgs[3..];
        }

        var split = trimmedArgs.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var target = split.ElementAtOrDefault(0) ?? string.Empty;
        var text = split.ElementAtOrDefault(1) ?? string.Empty;

        return new WriteCommand
        {
            Player = player,
            Target = target,
            Text = text
        };
    }
}

public class WriteHandler(World.World world, Searching.TargetResolver resolver) : IHandler<WriteCommand>
{
    public Task<CommandResult> Handle(WriteCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();

        var target = cmd.Target.Trim();
        var text = cmd.Text.Trim();

        if (string.IsNullOrWhiteSpace(target))
        {
            result.Add(cmd.Player, new SystemMessageEvent("Write on what?"));

            return Task.FromResult(result);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            result.Add(cmd.Player, new SystemMessageEvent("Write what?"));

            return Task.FromResult(result);
        }

        var room = world.GetLocationOrThrow(cmd.Player);

        var search = resolver.FindNearbyObject(cmd.Player, room, target);

        switch (search.Status)
        {
            case SearchStatus.NotFound:
                result.Add(cmd.Player, new ItemNotFoundEvent(target));

                break;

            case SearchStatus.IndexOutOfRange:
                result.Add(cmd.Player, new SystemMessageEvent($"You can't see a '{target}' here."));

                break;

            case SearchStatus.Ambiguous:
                result.Add(cmd.Player, new AmbiguousInputEvent(target, search.Candidates));

                break;

            case SearchStatus.Found:
                var item = search.Match!;

                if (!item.Flags.HasFlag(ObjectFlags.Writeable))
                {
                    result.Add(cmd.Player, new SystemMessageEvent("You can't write on that."));
                    return Task.FromResult(result);
                }

                item.WriteText(text);

                var writeEvent = new ObjectWrittenOnEvent(cmd.Player, item, text);
                result.Add(cmd.Player, writeEvent);

                if (item.Location is { } location && ReferenceEquals(location, room))
                {
                    result.BroadcastToAllButPlayer(room, cmd.Player, writeEvent);
                }

                break;
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