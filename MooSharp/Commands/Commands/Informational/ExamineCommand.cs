using System.Text;
using MooSharp.Messaging;

namespace MooSharp;

public class ExamineCommand : CommandBase<ExamineCommand>
{
    public required Player Player { get; init; }
    public required string Target { get; init; }
}

public class ExamineCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["examine", "ex", "x", "view", "look"];

    public string Description => "Inspect yourself, an item, or the room. Usage: examine <target>.";
    public CommandCategory Category => CommandCategory.General;

    public ICommand Create(Player player, string args)
        => new ExamineCommand
        {
            Player = player,
            Target = args
        };
}

public class ExamineHandler(World world, TargetResolver resolver) : IHandler<ExamineCommand>
{
    public Task<CommandResult> Handle(ExamineCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();

        var player = cmd.Player;

        if (string.IsNullOrWhiteSpace(cmd.Target))
        {
            var currentLocation = world.GetLocationOrThrow(player);

            result.Add(player, new RoomDescriptionEvent(currentLocation.DescribeFor(player, useLongDescription: true)));

            return Task.FromResult(result);
        }

        var current = world.GetLocationOrThrow(player);

        var search = resolver.FindObjects(current.Contents, cmd.Target);

        if (search.IsSelf)
        {
            var inventory = player.Inventory
                .ToList();

            result.Add(player, new SelfExaminedEvent(player, inventory));

            return Task.FromResult(result);
        }

        switch (search.Status)
        {
            case SearchStatus.NotFound:
                result.Add(player, new ItemNotFoundEvent(cmd.Target));
                break;

            case SearchStatus.IndexOutOfRange:
                result.Add(player, new SystemMessageEvent($"You can't see a '{cmd.Target}' here."));
                break;

            case SearchStatus.Ambiguous:
                result.Add(player, new AmbiguousInputEvent(cmd.Target, search.Candidates));
                break;

            case SearchStatus.Found:
                result.Add(player, new ObjectExaminedEvent(search.Match!));
                break;
        }

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

public record ItemNotFoundEvent(string ItemName) : IGameEvent;

public class ItemNotFoundEventFormatter : IGameEventFormatter<ItemNotFoundEvent>
{
    public string FormatForActor(ItemNotFoundEvent gameEvent) => $"There is no {gameEvent.ItemName} here.";

    public string FormatForObserver(ItemNotFoundEvent gameEvent) => FormatForActor(gameEvent);
}
