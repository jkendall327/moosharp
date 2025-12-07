using MooSharp.Actors.Players;
using MooSharp.Actors.Rooms;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;

namespace MooSharp.Commands.Commands.Creative;

public class DescribeCommand : CommandBase<DescribeCommand>
{
    public required Player Player { get; init; }
    public required string Target { get; init; }
    public required string Description { get; init; }
}

public class DescribeCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["@describe", "@desc"];

    public string Description =>
        "Update a room description. Usage: @describe here <description> or @describe <exit> <description>.";

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        var args = ctx.GetRemainingText();
        var trimmed = args.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            command = new DescribeCommand
            {
                Player = ctx.Player,
                Target = string.Empty,
                Description = string.Empty
            };

            return null;
        }

        var firstSpace = trimmed.IndexOf(' ');

        if (firstSpace < 0)
        {
            command = new DescribeCommand
            {
                Player = ctx.Player,
                Target = trimmed,
                Description = string.Empty
            };
        }

        var target = trimmed[..firstSpace];

        var description = trimmed[(firstSpace + 1)..]
            .Trim();

        if (description.StartsWith('"') && description.EndsWith('"') && description.Length > 1)
        {
            description = description[1..^1];
        }

        command = new DescribeCommand
        {
            Player = ctx.Player,
            Target = target,
            Description = description
        };

        return null;
    }

    public CommandCategory Category => CommandCategory.General;
}

public class DescribeHandler(World.World world) : IHandler<DescribeCommand>
{
    public async Task<CommandResult> Handle(DescribeCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();

        var player = cmd.Player;

        if (string.IsNullOrWhiteSpace(cmd.Target) || string.IsNullOrWhiteSpace(cmd.Description))
        {
            result.Add(player,
                new SystemMessageEvent("Usage: @describe here <description> or @describe <exit> <description>."));

            return result;
        }

        var currentRoom = world.GetLocationOrThrow(player);

        var targetRoom = GetTargetRoom(currentRoom, cmd.Target);

        if (targetRoom is null)
        {
            result.Add(player, new ExitNotFoundEvent(cmd.Target));

            return result;
        }

        if (!targetRoom.IsOwnedBy(player))
        {
            result.Add(player, new SystemMessageEvent("You can only describe rooms you created."));

            return result;
        }

        await world.UpdateRoomDescriptionAsync(targetRoom, cmd.Description, cancellationToken);

        result.Add(player, new RoomDescriptionUpdatedEvent(targetRoom));

        return result;
    }

    private Room? GetTargetRoom(Room currentRoom, string target)
    {
        if (string.Equals(target, "here", StringComparison.OrdinalIgnoreCase))
        {
            return currentRoom;
        }

        if (!currentRoom.Exits.TryGetValue(target, out var exitRoomId))
        {
            return null;
        }

        return world.Rooms.GetValueOrDefault(exitRoomId);
    }
}

public record RoomDescriptionUpdatedEvent(Room Room) : IGameEvent;

public class RoomDescriptionUpdatedEventFormatter : IGameEventFormatter<RoomDescriptionUpdatedEvent>
{
    public string FormatForActor(RoomDescriptionUpdatedEvent gameEvent) =>
        $"You update the description for {gameEvent.Room.Name}.";

    public string? FormatForObserver(RoomDescriptionUpdatedEvent gameEvent) => null;
}