using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace MooSharp;

public readonly record struct RoomId(string Value)
{
    public override string ToString() => Value;
    public static implicit operator RoomId(string value) => new(value);
}

public class RoomIdJsonConverter : JsonConverter<RoomId>
{
    public override RoomId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, RoomId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}

public class Room : IContainer
{
    public RoomId Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    private readonly List<Object> _contents = new();
    public IReadOnlyCollection<Object> Contents => _contents;
    public Dictionary<string, RoomId> Exits { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<Player> PlayersInRoom { get; } = new();

    public SearchResult FindObjects(string query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var match = Regex.Match(query, @"^(\d+)\.(.+)|(.+)\s+(\d+)$");
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

        var candidates = Contents
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

    public Object? FindObject(string keyword)
    {
        var search = FindObjects(keyword);
        return search.Status == SearchStatus.Found ? search.Match : null;
    }

    public string DescribeFor(Player player)
    {
        var sb = new StringBuilder();

        sb.AppendLine(Description);

        var otherPlayers = PlayersInRoom.Select(s => s.Username).Except([player.Username]);

        sb.AppendLine($"{string.Join(", ", otherPlayers)} are here.");

        var availableExits = Exits.Select(s => s.Key);
        var availableExitsMessage = $"Available exits: {string.Join(", ", availableExits)}";

        sb.AppendLine(availableExitsMessage);

        return sb.ToString();
    }

    public override string ToString() => Id.ToString();

    IReadOnlyCollection<Object> IContainer.Contents => _contents;

    void IContainer.AddToContents(Object item) => _contents.Add(item);

    bool IContainer.RemoveFromContents(Object item) => _contents.Remove(item);
}

public enum SearchStatus
{
    Found,
    NotFound,
    Ambiguous,
    IndexOutOfRange
}

public class SearchResult
{
    public SearchStatus Status { get; init; } = SearchStatus.Found;

    public Object? Match { get; init; }

    public List<Object> Candidates { get; init; } = [];
}
