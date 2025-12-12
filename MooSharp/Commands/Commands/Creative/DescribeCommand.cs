using MooSharp.Actors.Players;
using MooSharp.Actors.Rooms;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;
using MooSharp.Commands.Searching;

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
        "Update a room description or your own. Usage: @describe here <description>, @describe <exit> <description>, or @describe me <description>.";

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
        var target = firstSpace < 0 ? trimmed : trimmed[..firstSpace];

        var description = firstSpace < 0
            ? string.Empty
            : trimmed[(firstSpace + 1)..]
                .Trim();

        if (description.StartsWith('"') && description.EndsWith('"') && description.Length > 1)
        {
            description = description[1..^1];
        }

        if (IsSelfTarget(target, ctx.Player))
        {
            command = new DescribeSelfCommand
            {
                Player = ctx.Player,
                NewDescription = description
            };

            return null;
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

    private static bool IsSelfTarget(string target, Player player)
    {
        return string.Equals(target, "me", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(target, "self", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(target, "myself", StringComparison.OrdinalIgnoreCase) ||
               target.Equals(player.Username, StringComparison.OrdinalIgnoreCase);
    }
}

public class DescribeHandler(World.World world, TargetResolver resolver) : IHandler<DescribeCommand>
{
    public async Task<CommandResult> Handle(DescribeCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();

        var player = cmd.Player;

        if (string.IsNullOrWhiteSpace(cmd.Target) || string.IsNullOrWhiteSpace(cmd.Description))
        {
            result.Add(player,
                new SystemMessageEvent(
                    "Usage: @describe here <description>, @describe <exit> <description>, or @describe me <description>."));

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

        var exitSearch = resolver.FindExit(currentRoom, target);

        return exitSearch.Status == SearchStatus.Found
            ? world.Rooms.GetValueOrDefault(exitSearch.Match!.Destination)
            : null;
    }
}

public record RoomDescriptionUpdatedEvent(Room Room) : IGameEvent;

public class RoomDescriptionUpdatedEventFormatter : IGameEventFormatter<RoomDescriptionUpdatedEvent>
{
    public string FormatForActor(RoomDescriptionUpdatedEvent gameEvent) =>
        $"You update the description for {gameEvent.Room.Name}.";

    public string? FormatForObserver(RoomDescriptionUpdatedEvent gameEvent) => null;
}