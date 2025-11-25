using System.Text.RegularExpressions;

namespace MooSharp;

public static class ContainerSearchExtensions
{
    private static readonly Regex SearchRegex = new(@"^(\d+)\.(.+)|(.+)\s+(\d+)$", RegexOptions.Compiled);
    
    public static SearchResult FindObjects(this IReadOnlyCollection<Object> contents, string query)
    {
        ArgumentNullException.ThrowIfNull(query);

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
            .Where(o =>
                o.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase)
                || o.Keywords.Contains(targetName, StringComparer.OrdinalIgnoreCase)
                || o.Name.Contains(targetName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (targetIndex.HasValue)
        {
            var adjustedIndex = targetIndex.Value - 1;
            if (adjustedIndex >= 0 && adjustedIndex < candidates.Count)
            {
                return new SearchResult { Match = candidates[adjustedIndex] };
            }

            return new SearchResult { Status = SearchStatus.IndexOutOfRange };
        }

        return candidates.Count switch
        {
            0 => new SearchResult { Status = SearchStatus.NotFound },
            1 => new SearchResult { Match = candidates[0] },
            _ => new SearchResult { Status = SearchStatus.Ambiguous, Candidates = candidates }
        };
    }
}
