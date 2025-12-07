using MooSharp.Actors.Players;
using MooSharp.Actors.Rooms;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Scripting;

public class VerbScriptResolver : IVerbScriptResolver
{
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
