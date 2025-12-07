using MooSharp.Actors.Players;
using MooSharp.Actors.Rooms;
using MooSharp.Commands.Commands.Informational;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;
using MooSharp.Commands.Searching;

namespace MooSharp.Commands.Commands.Creative;

public class RenameCommand : CommandBase<RenameCommand>
{
    public required Player Player { get; init; }
    public required string Target { get; init; }
    public required string NewName { get; init; }
}

public class RenameCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["@rename"];

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        var args = ctx.GetRemainingText();
        var trimmed = args.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            command = new RenameCommand
            {
                Player = ctx.Player,
                Target = string.Empty,
                NewName = string.Empty
            };
        }

        var parts = trimmed.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        command = new RenameCommand
        {
            Player = ctx.Player,
            Target = parts.ElementAtOrDefault(0) ?? string.Empty,
            NewName = parts.ElementAtOrDefault(1) ?? string.Empty
        };

        return null;
    }

    public CommandCategory Category => CommandCategory.General;

    public string Description => "Rename a room or item you created. Usage: @rename <target> <new name>.";
}

public class RenameHandler(World.World world, TargetResolver resolver) : IHandler<RenameCommand>
{
    public async Task<CommandResult> Handle(RenameCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();

        var target = cmd.Target.Trim();
        var newName = cmd.NewName.Trim();

        if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(newName))
        {
            result.Add(cmd.Player, new SystemMessageEvent("Usage: @rename <target> <new name>."));

            return result;
        }

        var currentRoom = world.GetLocationOrThrow(cmd.Player);

        var targetRoom = GetTargetRoom(currentRoom, target);

        if (targetRoom is not null)
        {
            if (!targetRoom.IsOwnedBy(cmd.Player))
            {
                result.Add(cmd.Player, new SystemMessageEvent("You can only rename rooms you created."));

                return result;
            }

            var renameEvent = new RoomRenamedEvent(cmd.Player, targetRoom.Name, newName);

            await world.RenameRoomAsync(targetRoom, newName, cancellationToken);

            result.Add(cmd.Player, renameEvent);

            if (ReferenceEquals(targetRoom, currentRoom))
            {
                result.BroadcastToAllButPlayer(currentRoom, cmd.Player, renameEvent);
            }

            return result;
        }

        var search = resolver.FindNearbyObject(cmd.Player, currentRoom, target);

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

                if (!item.IsOwnedBy(cmd.Player))
                {
                    result.Add(cmd.Player, new SystemMessageEvent("You can only rename items you created."));

                    break;
                }

                var renameEvent = new ObjectRenamedEvent(cmd.Player, item.Name, newName);

                await world.RenameObjectAsync(item, newName, cancellationToken);

                result.Add(cmd.Player, renameEvent);

                if (item.Location is { } location && ReferenceEquals(location, currentRoom))
                {
                    result.BroadcastToAllButPlayer(location, cmd.Player, renameEvent);
                }

                break;
        }

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

public record RoomRenamedEvent(Player Player, string OldName, string NewName) : IGameEvent;

public class RoomRenamedEventFormatter : IGameEventFormatter<RoomRenamedEvent>
{
    public string FormatForActor(RoomRenamedEvent gameEvent) =>
        $"You rename {gameEvent.OldName} to '{gameEvent.NewName}'.";

    public string FormatForObserver(RoomRenamedEvent gameEvent) =>
        $"{gameEvent.Player.Username} renames {gameEvent.OldName} to '{gameEvent.NewName}'.";
}

public record ObjectRenamedEvent(Player Player, string OldName, string NewName) : IGameEvent;

public class ObjectRenamedEventFormatter : IGameEventFormatter<ObjectRenamedEvent>
{
    public string FormatForActor(ObjectRenamedEvent gameEvent) =>
        $"You rename the {gameEvent.OldName} to '{gameEvent.NewName}'.";

    public string FormatForObserver(ObjectRenamedEvent gameEvent) =>
        $"{gameEvent.Player.Username} renames the {gameEvent.OldName} to '{gameEvent.NewName}'.";
}