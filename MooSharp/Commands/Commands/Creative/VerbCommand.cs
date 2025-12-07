using Microsoft.Extensions.Options;
using MooSharp.Actors.Objects;
using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;
using MooSharp.Scripting;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Commands.Commands.Creative;

public class VerbCommand : CommandBase<VerbCommand>
{
    public required Player Player { get; init; }
    public required string ObjectTarget { get; init; }
    public required string VerbName { get; init; }
}

public class VerbCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["@verb"];
    public CommandCategory Category => CommandCategory.General;

    public string Description => "Define a verb on an object you own. Usage: @verb object:verbname.";

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        var args = ctx.GetRemainingText().Trim();

        var parts = args.Split(':');
        if (parts.Length != 2)
        {
            command = null;
            return "Usage: @verb object:verbname.";
        }

        var objectName = parts[0].Trim();
        var verbName = parts[1].Trim();

        if (string.IsNullOrWhiteSpace(objectName) || string.IsNullOrWhiteSpace(verbName))
        {
            command = null;
            return "Usage: @verb object:verbname.";
        }

        command = new VerbCommand
        {
            Player = ctx.Player,
            ObjectTarget = objectName,
            VerbName = verbName
        };

        return null;
    }
}

public record VerbDefinedEvent(Player Player, Object Item, string VerbName) : IGameEvent;

public class VerbDefinedEventFormatter : IGameEventFormatter<VerbDefinedEvent>
{
    public string FormatForActor(VerbDefinedEvent gameEvent) =>
        $"You define verb '{gameEvent.VerbName}' on '{gameEvent.Item.Name}'. (Stub code added - edit later.)";

    public string? FormatForObserver(VerbDefinedEvent gameEvent) => null;
}

public record VerbAlreadyExistsEvent(Object Item, string VerbName) : IGameEvent;

public class VerbAlreadyExistsEventFormatter : IGameEventFormatter<VerbAlreadyExistsEvent>
{
    public string FormatForActor(VerbAlreadyExistsEvent gameEvent) =>
        $"'{gameEvent.Item.Name}' already has a verb named '{gameEvent.VerbName}'.";

    public string? FormatForObserver(VerbAlreadyExistsEvent gameEvent) => null;
}

public class VerbHandler(World.World world, IOptions<LuaScriptOptions> options) : IHandler<VerbCommand>
{
    private readonly LuaScriptOptions _options = options.Value;

    public Task<CommandResult> Handle(VerbCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var player = cmd.Player;
        var room = world.GetLocationOrThrow(player);

        // Find the object in room or inventory
        var target = FindObject(player, room, cmd.ObjectTarget);

        if (target is null)
        {
            result.Add(player, new SystemMessageEvent($"Cannot find '{cmd.ObjectTarget}'."));
            return Task.FromResult(result);
        }

        // Check ownership
        if (!target.IsOwnedBy(player))
        {
            result.Add(player, new SystemMessageEvent($"You don't own '{target.Name}'."));
            return Task.FromResult(result);
        }

        // Check if verb already exists
        if (target.HasVerb(cmd.VerbName))
        {
            result.Add(player, new VerbAlreadyExistsEvent(target, cmd.VerbName));
            return Task.FromResult(result);
        }

        if (target.Verbs.Count >= _options.MaxVerbsPerObject)
        {
            result.Add(player, new SystemMessageEvent(
                $"'{target.Name}' already has the maximum of {_options.MaxVerbsPerObject} verbs."));

            return Task.FromResult(result);
        }

        // Create a stub verb
        var verbScript = VerbScript.CreateStub(cmd.VerbName, player.Username);
        target.Verbs[cmd.VerbName] = verbScript;

        // Mark the room/player as modified
        if (target.Location is not null)
        {
            world.MarkRoomModified(target.Location);
        }

        result.Add(player, new VerbDefinedEvent(player, target, cmd.VerbName));

        return Task.FromResult(result);
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
