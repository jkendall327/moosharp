using System.Text.RegularExpressions;
using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Commands.Commands.Creative;

public class SetPropertyCommand : CommandBase<SetPropertyCommand>
{
    public required Player Player { get; init; }
    public required string ObjectTarget { get; init; }
    public required string PropertyName { get; init; }
    public required string PropertyValue { get; init; }
}

public class SetPropertyCommandDefinition : ICommandDefinition
{
    private static readonly Regex SetPattern = new(@"^(\w+)\.(\w+)\s*=\s*(.+)$", RegexOptions.Compiled);

    public IReadOnlyCollection<string> Verbs { get; } = ["@set"];
    public CommandCategory Category => CommandCategory.General;

    public string Description => "Set a dynamic property on an object you own. Usage: @set object.property = value.";

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        var args = ctx.GetRemainingText().Trim();

        var match = SetPattern.Match(args);
        if (!match.Success)
        {
            command = null;
            return "Usage: @set object.property = value.";
        }

        command = new SetPropertyCommand
        {
            Player = ctx.Player,
            ObjectTarget = match.Groups[1].Value,
            PropertyName = match.Groups[2].Value,
            PropertyValue = match.Groups[3].Value.Trim().Trim('"', '\'')
        };

        return null;
    }
}

public record PropertySetEvent(Player Player, Object Item, string PropertyName, string Value) : IGameEvent;

public class PropertySetEventFormatter : IGameEventFormatter<PropertySetEvent>
{
    public string FormatForActor(PropertySetEvent gameEvent) =>
        $"You set {gameEvent.Item.Name}.{gameEvent.PropertyName} = {gameEvent.Value}.";

    public string? FormatForObserver(PropertySetEvent gameEvent) => null;
}

public class SetPropertyHandler(World.World world) : IHandler<SetPropertyCommand>
{
    public Task<CommandResult> Handle(SetPropertyCommand cmd, CancellationToken cancellationToken = default)
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

        // Parse the value
        object value = ParseValue(cmd.PropertyValue);

        target.Properties[cmd.PropertyName] = value;

        // Mark the room/player as modified
        if (target.Location is not null)
        {
            world.MarkRoomModified(target.Location);
        }

        result.Add(player, new PropertySetEvent(player, target, cmd.PropertyName, cmd.PropertyValue));

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

    private static object ParseValue(string value)
    {
        // Try to parse as number
        if (double.TryParse(value, out var number))
        {
            return number;
        }

        // Try to parse as boolean
        if (bool.TryParse(value, out var boolean))
        {
            return boolean;
        }

        // Default to string
        return value;
    }
}
