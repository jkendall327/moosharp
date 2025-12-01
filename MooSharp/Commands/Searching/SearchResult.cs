namespace MooSharp;

public class SearchResult
{
    public SearchStatus Status { get; init; } = SearchStatus.Found;

    public Object? Match { get; init; }

    public List<Object> Candidates { get; init; } = [];

    public bool IsSelf { get; init; }
}
