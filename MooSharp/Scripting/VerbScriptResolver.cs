using MooSharp.Actors.Players;
using MooSharp.Actors.Rooms;
using MooSharp.Commands.Commands.Scripting;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Scripting;

public class VerbScriptResolver : IVerbScriptResolver
{
    public ScriptCommand? TryResolveCommand(Player player, Room room, string input)
    {
        // Parse the input: first word is verb, rest is target
        var parts = input.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return null;
        }

        var verb = parts[0];
        var targetText = parts.Length > 1 ? parts[1] : string.Empty;

        var resolution = Resolve(player, room, verb, targetText);

        if (!resolution.Found || resolution.TargetObject is null || resolution.Script is null)
        {
            return null;
        }

        // Parse remaining arguments after the target
        var arguments = Array.Empty<string>();
        if (parts.Length > 1)
        {
            var argsStart = parts[1].IndexOf(resolution.TargetObject.Name, StringComparison.OrdinalIgnoreCase);
            if (argsStart >= 0)
            {
                var afterTarget = parts[1][(argsStart + resolution.TargetObject.Name.Length)..].Trim();
                if (!string.IsNullOrEmpty(afterTarget))
                {
                    arguments = afterTarget.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                }
            }
        }

        return new ScriptCommand
        {
            Player = player,
            TargetObject = resolution.TargetObject,
            Script = resolution.Script,
            VerbName = verb,
            Arguments = arguments
        };
    }

    public VerbResolutionResult Resolve(Player player, Room room, string verb, string targetText)
    {
        // If there's no target text, we can't find an object
        if (string.IsNullOrWhiteSpace(targetText))
        {
            // Try to find any object in room or inventory that has this verb
            var anyMatch = FindObjectWithVerb(player, room, verb);
            if (anyMatch is not null && anyMatch.Verbs.TryGetVerb(verb, out var script) && script is not null)
            {
                return VerbResolutionResult.Success(anyMatch, script);
            }

            return VerbResolutionResult.NotFound();
        }

        // First, try to find the object in the room
        var targetInRoom = FindObjectByName(room.Contents, targetText);
        if (targetInRoom is not null)
        {
            if (targetInRoom.Verbs.TryGetVerb(verb, out var script) && script is not null)
            {
                return VerbResolutionResult.Success(targetInRoom, script);
            }

            // Object found but doesn't have this verb
            return VerbResolutionResult.NotFound();
        }

        // Next, try player's inventory
        var targetInInventory = FindObjectByName(player.Inventory, targetText);
        if (targetInInventory is not null)
        {
            if (targetInInventory.Verbs.TryGetVerb(verb, out var script) && script is not null)
            {
                return VerbResolutionResult.Success(targetInInventory, script);
            }

            // Object found but doesn't have this verb
            return VerbResolutionResult.NotFound();
        }

        // Object not found anywhere
        return VerbResolutionResult.NotFound();
    }

    private static Object? FindObjectByName(IEnumerable<Object> objects, string targetText)
    {
        var normalizedTarget = targetText.Trim().ToLowerInvariant();

        // Try exact match first
        var exactMatch = objects.FirstOrDefault(o =>
            string.Equals(o.Name, targetText, StringComparison.OrdinalIgnoreCase));

        if (exactMatch is not null)
        {
            return exactMatch;
        }

        // Try partial match (object name contains target or vice versa)
        var partialMatch = objects.FirstOrDefault(o =>
            o.Name.Contains(targetText, StringComparison.OrdinalIgnoreCase) ||
            targetText.Contains(o.Name, StringComparison.OrdinalIgnoreCase));

        if (partialMatch is not null)
        {
            return partialMatch;
        }

        // Try keyword match
        var keywordMatch = objects.FirstOrDefault(o =>
            o.Keywords.Any(k => string.Equals(k, targetText, StringComparison.OrdinalIgnoreCase)));

        return keywordMatch;
    }

    private static Object? FindObjectWithVerb(Player player, Room room, string verb)
    {
        // Check room contents first
        var inRoom = room.Contents.FirstOrDefault(o => o.HasVerb(verb));
        if (inRoom is not null)
        {
            return inRoom;
        }

        // Check inventory
        return player.Inventory.FirstOrDefault(o => o.HasVerb(verb));
    }
}
