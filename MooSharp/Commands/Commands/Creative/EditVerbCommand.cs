using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;
using MooSharp.Features.Editor;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Commands.Commands.Creative;

public class EditVerbCommand : CommandBase<EditVerbCommand>
{
    public required Player Player { get; init; }
    public required string ObjectTarget { get; init; }
    public required string VerbName { get; init; }
}

public class EditVerbCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["@edit"];
    public CommandCategory Category => CommandCategory.General;

    public string Description => "Edit a verb's Lua code. Usage: @edit object:verbname";

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        var arg = ctx.Pop();

        if (arg is null)
        {
            command = null;
            return "Usage: @edit object:verbname";
        }

        var parts = arg.Split(':');
        if (parts.Length != 2)
        {
            command = null;
            return "Usage: @edit object:verbname";
        }

        var objectName = parts[0].Trim();
        var verbName = parts[1].Trim();

        if (string.IsNullOrWhiteSpace(objectName) || string.IsNullOrWhiteSpace(verbName))
        {
            command = null;
            return "Usage: @edit object:verbname";
        }

        command = new EditVerbCommand
        {
            Player = ctx.Player,
            ObjectTarget = objectName,
            VerbName = verbName
        };

        return null;
    }
}

public class EditVerbHandler(
    World.World world,
    IEditorModeService editorService,
    IEditorModeNotifier notifier) : IHandler<EditVerbCommand>
{
    public async Task<CommandResult> Handle(EditVerbCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var player = cmd.Player;
        var room = world.GetLocationOrThrow(player);

        // Find the object
        var target = FindObject(player, room, cmd.ObjectTarget);

        if (target is null)
        {
            result.Add(player, new SystemMessageEvent($"Cannot find '{cmd.ObjectTarget}'."));
            return result;
        }

        // Check ownership
        if (!target.IsOwnedBy(player))
        {
            result.Add(player, new SystemMessageEvent($"You don't own '{target.Name}'."));
            return result;
        }

        // Check if verb exists
        if (!target.HasVerb(cmd.VerbName))
        {
            result.Add(player, new VerbNotFoundEvent(target.Name, cmd.VerbName));
            return result;
        }

        // Check if already in editor mode
        if (editorService.IsInEditorMode(player.Id.Value))
        {
            result.Add(player, new SystemMessageEvent("You are already in editor mode. Type '.' to save or '@abort' to cancel."));
            return result;
        }

        // Start editor session
        editorService.StartSession(player.Id.Value, target.Id, target.Name, cmd.VerbName);

        // Notify client about mode change
        await notifier.NotifyEditorModeEnteredAsync(player.Id.Value, target.Name, cmd.VerbName, cancellationToken);

        result.Add(player, new EditorModeEnteredEvent(target.Name, cmd.VerbName));

        return result;
    }

    private static Object? FindObject(Player player, MooSharp.Actors.Rooms.Room room, string name)
    {
        // Check room first
        var inRoom = room.Contents.FirstOrDefault(o =>
            string.Equals(o.Name, name, StringComparison.OrdinalIgnoreCase) ||
            o.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

        if (inRoom is not null)
        {
            return inRoom;
        }

        // Check inventory
        return player.Inventory.FirstOrDefault(o =>
            string.Equals(o.Name, name, StringComparison.OrdinalIgnoreCase) ||
            o.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
    }
}
