using System.Text.RegularExpressions;

namespace MooSharp;

/// <summary>
/// Binds raw text to game objects.
/// </summary>
public partial class TargetResolver
{
    private static readonly Regex SearchRegex = CreateSearchRegex();

    public SearchResult FindNearbyObject(Player player, Room room, string target)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(target);

        if (IsSelfReference(target))
        {
            return new()
            {
                IsSelf = true
            };
        }

        var result = FindObjects(player.Inventory, target);

        return result.Status is SearchStatus.NotFound ? FindObjects(room.Contents, target) : result;
    }

    public SearchResult FindObjects(IReadOnlyCollection<Object> contents, string query)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        if (IsSelfReference(query))
        {
            return new()
            {
                IsSelf = true
            };
        }

        var match = SearchRegex.Match(query);
        var targetName = query;
        int? targetIndex = null;

        if (match.Success)
        {
            var part1 = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value;
            var part2 = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[4].Value;

            targetName = part1.Trim();

            if (int.TryParse(part2, out var index))
            {
                targetIndex = index;
            }
        }

        var candidates = contents
            .Where(o => o.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase) ||
                        o.Keywords.Contains(targetName, StringComparer.OrdinalIgnoreCase) ||
                        o.Name.Contains(targetName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (targetIndex.HasValue)
        {
            var adjustedIndex = targetIndex.Value - 1;

            if (adjustedIndex >= 0 && adjustedIndex < candidates.Count)
            {
                return new()
                {
                    Match = candidates[adjustedIndex]
                };
            }

            return new()
            {
                Status = SearchStatus.IndexOutOfRange
            };
        }

        return candidates.Count switch
        {
            0 => new()
            {
                Status = SearchStatus.NotFound
            },
            1 => new()
            {
                Match = candidates[0]
            },
            _ => new()
            {
                Status = SearchStatus.Ambiguous,
                Candidates = candidates
            }
        };
    }

    private static bool IsSelfReference(string target)
        => string.Equals(target, "me", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(target, "self", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(target, "myself", StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(@"^(\d+)\.(.+)|(.+)\s+(\d+)$", RegexOptions.Compiled)]
    private static partial Regex CreateSearchRegex();
}
