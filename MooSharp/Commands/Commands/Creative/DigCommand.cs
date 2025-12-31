using MooSharp.Actors.Players;
using MooSharp.Actors.Rooms;
using MooSharp.Commands.Commands.Items;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;

namespace MooSharp.Commands.Commands.Creative;

public class DigCommand : CommandBase<DigCommand>
{
    public required Player Player { get; init; }
    public required string RoomName { get; init; }
}

public class DigCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["@dig"];
    public CommandCategory Category => CommandCategory.General;

    public string Description => "Create a new room connected to your current one. Usage: @dig to <room name>.";

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        binder.ConsumePreposition(ctx, "to");
        var name = ctx.GetRemainingText();

        if (string.IsNullOrWhiteSpace(name))
        {
            name = "Unfinished room";
        }

        command = new DigCommand
        {
            Player = ctx.Player,
            RoomName = name
        };

        return null;
    }
}

public record RoomCreatedEvent(Player Player, Room Room) : IGameEvent;

public class RoomCreatedEventFormatter : IGameEventFormatter<RoomCreatedEvent>
{
    public string FormatForActor(RoomCreatedEvent gameEvent) => $"You dig a passage to '{gameEvent.Room.Name}'.";

    public string FormatForObserver(RoomCreatedEvent gameEvent) => $"{gameEvent.Player.Username} digs a passage to '{gameEvent.Room.Name}'.";
}

public record RoomAlreadyExistsEvent(string Slug) : IGameEvent;

public class RoomAlreadyExistsEventFormatter : IGameEventFormatter<RoomAlreadyExistsEvent>
{
    public string FormatForActor(RoomAlreadyExistsEvent gameEvent) => $"A room with the slug '{gameEvent.Slug}' already exists.";
    public string? FormatForObserver(RoomAlreadyExistsEvent gameEvent) => null;
}

public class DigHandler(World.World world, SlugCreator slugCreator) : IHandler<DigCommand>
{
    private const string DefaultEnterText = "You step inside.";
    private const string DefaultExitText = "You leave the room.";

    public async Task<CommandResult> Handle(DigCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var player = cmd.Player;

        var currentRoom = world.GetLocationOrThrow(player);

        if (string.IsNullOrWhiteSpace(cmd.RoomName))
        {
            result.Add(player, new SystemMessageEvent("Specify a room name to dig."));
            return result;
        }

        var slug = slugCreator.CreateSlug(cmd.RoomName);

        if (string.IsNullOrWhiteSpace(slug))
        {
            result.Add(player, new SystemMessageEvent("Unable to create a slug for that room name."));
            return result;
        }

        if (world.Rooms.ContainsKey(slug))
        {
            result.Add(player, new RoomAlreadyExistsEvent(slug));
            return result;
        }

        if (world.Rooms.Values.SelectMany(r => r.Exits.Select(e => e.Name)).Any(e => string.Equals(e, slug, StringComparison.OrdinalIgnoreCase)))
        {
            result.Add(player, new RoomAlreadyExistsEvent(slug));
            return result;
        }

        var description = $"A newly dug room branching from {currentRoom.Name}.";
        var longDescription =
            $"Freshly carved walls surround this new space extending from {currentRoom.Name}. Dust still hangs in the air.";

        var newRoom = await world.CreateRoomAsync(
            slug,
            cmd.RoomName,
            description,
            longDescription,
            DefaultEnterText,
            DefaultExitText,
            player.Username,
            cancellationToken);

        await world.AddExitAsync(currentRoom, newRoom, slug, cancellationToken);
        await world.AddExitAsync(newRoom, currentRoom, currentRoom.Id.Value, cancellationToken);

        result.Add(player, new RoomCreatedEvent(player, newRoom));

        return result;
    }
}
