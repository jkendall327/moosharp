using MooSharp.Actors.Objects;
using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Commands.Commands.Creative;

public class CreateObjectCommand : CommandBase<CreateObjectCommand>
{
    public required Player Player { get; init; }
    public required string ObjectName { get; init; }
}

public class CreateObjectCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["@create"];
    public CommandCategory Category => CommandCategory.General;

    public string Description => "Create a new programmable object in the current room. Usage: @create object \"Name\".";

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        // Expected format: @create object "Name" or @create object Name
        if (!binder.ConsumePreposition(ctx, "object"))
        {
            command = null;
            return "Usage: @create object \"Name\".";
        }

        var nameArg = ctx.GetRemainingText();

        if (string.IsNullOrWhiteSpace(nameArg))
        {
            command = null;
            return "You must specify a name for the object.";
        }

        command = new CreateObjectCommand
        {
            Player = ctx.Player,
            ObjectName = nameArg
        };

        return null;
    }
}

public record ObjectCreatedEvent(Player Player, Object Item) : IGameEvent;

public class ObjectCreatedEventFormatter : IGameEventFormatter<ObjectCreatedEvent>
{
    public string FormatForActor(ObjectCreatedEvent gameEvent) =>
        $"You create '{gameEvent.Item.Name}'.";

    public string? FormatForObserver(ObjectCreatedEvent gameEvent) =>
        $"{gameEvent.Player.Username} creates '{gameEvent.Item.Name}'.";
}

public class CreateObjectHandler(World.World world) : IHandler<CreateObjectCommand>
{
    public Task<CommandResult> Handle(CreateObjectCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var player = cmd.Player;
        var room = world.GetLocationOrThrow(player);

        var newObject = new Object
        {
            Id = ObjectId.New(),
            Name = cmd.ObjectName,
            Description = $"A newly created object called '{cmd.ObjectName}'.",
            CreatorUsername = player.Username,
            Properties = new(),
            Verbs = new()
        };

        newObject.MoveTo(room);

        // Mark the room as modified so it gets saved
        world.MarkRoomModified(room);

        result.Add(player, new ObjectCreatedEvent(player, newObject));
        result.BroadcastToAllButPlayer(room, player, new ObjectCreatedEvent(player, newObject));

        return Task.FromResult(result);
    }
}
