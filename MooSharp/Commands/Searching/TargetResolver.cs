using System.Text.RegularExpressions;
using MooSharp.Actors;
using MooSharp.Actors.Players;
using MooSharp.Actors.Rooms;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Commands.Searching;

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

        var entityResult = FindEntities(contents, query, GetObjectTerms);

        return new SearchResult
        {
            Status = entityResult.Status,
            Match = entityResult.Match,
            Candidates = entityResult.Candidates,
            IsSelf = entityResult.IsSelf
        };
    }

    public SearchResult<IOpenable> FindOpenable(Player player, Room room, string query)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        var inventorySearch = FindEntities(player.Inventory.OfType<IOpenable>().ToList(), query, GetOpenableTerms);

        if (inventorySearch.Status != SearchStatus.NotFound)
        {
            return inventorySearch;
        }

        var roomSearch = FindEntities(room.Contents.OfType<IOpenable>().ToList(), query, GetOpenableTerms);

        if (roomSearch.Status != SearchStatus.NotFound)
        {
            return roomSearch;
        }

        var exitSearch = FindEntities(room.Exits.Where(e => !e.IsHidden).Cast<IOpenable>().ToList(), query, GetOpenableTerms);

        return exitSearch;
    }

    public SearchResult<Exit> FindExit(Room room, string query)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        return FindEntities(room.Exits.Where(e => !e.IsHidden).ToList(), query, GetExitTerms);
    }

    private static bool IsSelfReference(string target)
        => string.Equals(target, "me", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(target, "self", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(target, "myself", StringComparison.OrdinalIgnoreCase);

    private SearchResult<T> FindEntities<T>(IReadOnlyCollection<T> contents, string query, Func<T, IEnumerable<string>> termSelector)
    {
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
            .Where(o => termSelector(o)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Any(name => name.Equals(targetName, StringComparison.OrdinalIgnoreCase) ||
                             name.Contains(targetName, StringComparison.OrdinalIgnoreCase)))
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
            var _ => new()
            {
                Status = SearchStatus.Ambiguous,
                Candidates = candidates
            }
        };
    }

    private static IEnumerable<string> GetObjectTerms(Object obj)
    {
        return new[] { obj.Name }.Concat(obj.Keywords);
    }

    private static IEnumerable<string> GetOpenableTerms(IOpenable openable)
    {
        return openable switch
        {
            Exit exit => GetExitTerms(exit),
            Object obj => GetObjectTerms(obj),
            _ => new[] { openable.Name }
        };
    }

    private static IEnumerable<string> GetExitTerms(Exit exit)
    {
        return new[] { exit.Name }
            .Concat(exit.Aliases)
            .Concat(exit.Keywords);
    }

    [GeneratedRegex(@"^(\d+)\.(.+)|(.+)\s+(\d+)$", RegexOptions.Compiled)]
    private static partial Regex CreateSearchRegex();
}
