namespace MooSharp.Features.Autocomplete;

public class AutocompleteService(World.World world)
{
    public Task<AutocompleteOptions> GetAutocompleteOptions(Guid actorId, CancellationToken ct = default)
    {
        var player = world.TryGetPlayer(actorId);

        if (player is null)
        {
            return Task.FromResult(new AutocompleteOptions([], [], []));
        }

        var room = world.GetLocationOrThrow(player);

        var exits = room.Exits
            .Where(e => !e.IsHidden)
            .Select(e => e.Name);
        
        var inventory = player.Inventory.Select(item => item.Name);

        var items = room.Contents.Select(s => s.Name);
        
        var options = new AutocompleteOptions(exits.ToList(), inventory.ToList(), items.ToList());

        return Task.FromResult(options);
    }

    public string? GetMatch(AutocompleteOptions options, string command)
    {
        var candidates = options
            .Exits
            .Concat(options.InventoryItems)
            .Concat(options.ObjectsInRoom)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        (var prefix, var fragment) = SplitCommandInput(command);

        if (string.IsNullOrWhiteSpace(fragment))
        {
            return null;
        }

        var matches = candidates
            .Where(c => c.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            return null;
        }

        var completion = matches.Count == 1 ? matches[0] : FindCommonPrefix(matches, fragment);

        return $"{prefix}{completion}";
    }
    
    private static (string Prefix, string Fragment) SplitCommandInput(string input)
    {
        var lastSpaceIndex = input.LastIndexOf(' ');

        if (lastSpaceIndex == -1)
        {
            return (string.Empty, input);
        }

        var prefix = input[..(lastSpaceIndex + 1)];
        var fragment = input[(lastSpaceIndex + 1)..];

        return (prefix, fragment);
    }

    private static string FindCommonPrefix(List<string> options, string seed)
    {
        if (options.Count == 0)
        {
            return seed;
        }

        var comparison = StringComparison.OrdinalIgnoreCase;
        var prefix = seed;
        var reference = options[0];

        for (var i = seed.Length; i < reference.Length; i++)
        {
            var candidate = reference[..(i + 1)];

            if (options.Any(option => !option.StartsWith(candidate, comparison)))
            {
                break;
            }

            prefix = candidate;
        }

        return prefix;
    }
}