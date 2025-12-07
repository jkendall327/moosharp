using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Actors;

public interface IContainer
{
    IReadOnlyCollection<Object> Contents { get; }
    void AddToContents(Object item);
    void RemoveFromContents(Object item);
}
