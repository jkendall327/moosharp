namespace MooSharp.Commands.Searching;

public class SearchResult : SearchResult<MooSharp.Actors.Objects.Object>
{
}

public class SearchResult<T>
{
    public SearchStatus Status { get; init; } = SearchStatus.Found;

    public T? Match { get; init; }

    public List<T> Candidates { get; init; } = [];

    public bool IsSelf { get; init; }
}
