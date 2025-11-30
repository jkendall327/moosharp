using MooSharp.Messaging;

namespace MooSharp;

public class ReadCommand : CommandBase<ReadCommand>
{
    public required Player Player { get; init; }
    public required string Target { get; init; }
}

public class ReadCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["read"];

    public string Description => "Read writing on an item. Usage: read <item>.";

    public ICommand Create(Player player, string args) => new ReadCommand
    {
        Player = player,
        Target = args
    };
}

public class ReadHandler(World world, TargetResolver resolver) : IHandler<ReadCommand>
{
    public Task<CommandResult> Handle(ReadCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var target = cmd.Target.Trim();

        if (string.IsNullOrWhiteSpace(target))
        {
            result.Add(cmd.Player, new SystemMessageEvent("Read what?"));
            return Task.FromResult(result);
        }

        var room = world.GetPlayerLocation(cmd.Player)
            ?? throw new InvalidOperationException("Player has no known current location.");

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

                if (string.IsNullOrWhiteSpace(item.TextContent))
                {
                    result.Add(cmd.Player, new SystemMessageEvent($"There is nothing written on the {item.Name}."));
                    break;
                }

                result.Add(cmd.Player, new ObjectReadEvent(item, item.TextContent));
                break;
        }

        return Task.FromResult(result);
    }
}

public record ObjectReadEvent(Object Item, string Content) : IGameEvent;

public class ObjectReadEventFormatter : IGameEventFormatter<ObjectReadEvent>
{
    public string FormatForActor(ObjectReadEvent gameEvent)
        => $"It reads: \"{gameEvent.Content}\"";

    public string? FormatForObserver(ObjectReadEvent gameEvent) => null;
}
