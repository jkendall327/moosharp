namespace MooSharp.Actors;

public interface IContainer
{
    IReadOnlyCollection<Object> Contents { get; }
    void AddToContents(Object item);
    bool RemoveFromContents(Object item);
}
