using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Commands.Commands.Informational;

public class ReadCommand : CommandBase<ReadCommand>
{
    public required Player Player { get; init; }
    // Refactor: Holds the actual object now
    public required Object Target { get; init; }
}

public class ReadCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["read"];
    public CommandCategory Category => CommandCategory.General;
    public string Description => "Read writing on an item. Usage: read <item>.";

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        command = null;

        var bindResult = binder.BindNearbyObject(ctx);
        if (!bindResult.IsSuccess)
        {
            return bindResult.ErrorMessage;
        }

        command = new ReadCommand
        {
            Player = ctx.Player,
            Target = bindResult.Value!
        };

        return null;
    }
}

public class ReadHandler : IHandler<ReadCommand>
{
    public Task<CommandResult> Handle(ReadCommand cmd, CancellationToken ct = default)
    {
        var result = new CommandResult();
        var item = cmd.Target;

        if (string.IsNullOrWhiteSpace(item.TextContent))
        {
            result.Add(cmd.Player, new SystemMessageEvent($"There is nothing written on the {item.Name}."));
        }
        else
        {
            result.Add(cmd.Player, new ObjectReadEvent(item, item.TextContent));
        }

        return Task.FromResult(result);
    }
}

public record ObjectReadEvent(Object Item, string Content) : IGameEvent;

public class ObjectReadEventFormatter : IGameEventFormatter<ObjectReadEvent>
{
    public string FormatForActor(ObjectReadEvent gameEvent) => $"It reads: \"{gameEvent.Content}\"";
    public string? FormatForObserver(ObjectReadEvent gameEvent) => null;
}