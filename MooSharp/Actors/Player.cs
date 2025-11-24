using MooSharp.Messaging;

namespace MooSharp;

public readonly record struct PlayerId(Guid Value)
{
    public static PlayerId New() => new(Guid.CreateVersion7());
}

public class Player : IContainer
{
    private readonly List<Object> _inventory = [];

    public PlayerId Id { get; } = PlayerId.New();
    public required IPlayerConnection Connection { get; set; }
    public IReadOnlyCollection<Object> Inventory => _inventory;
    public required string Username { get; init; }

    IReadOnlyCollection<Object> IContainer.Contents => _inventory;

    void IContainer.AddToContents(Object item) => _inventory.Add(item);

    bool IContainer.RemoveFromContents(Object item) => _inventory.Remove(item);

    public override string ToString() => Username;
}