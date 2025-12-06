using MooSharp.Messaging;

namespace MooSharp.Actors;

public readonly record struct PlayerId(Guid Value)
{
    public static PlayerId New() => new(Guid.CreateVersion7());
}

public class Player : IContainer
{
    private readonly List<Object> _inventory = [];
    private readonly HashSet<string> _mutedChannels = new(StringComparer.OrdinalIgnoreCase);

    public PlayerId Id { get; } = PlayerId.New();
    
    public IReadOnlyCollection<Object> Inventory => _inventory;
    public required string Username { get; init; }
    public IReadOnlyCollection<string> MutedChannels => _mutedChannels;

    IReadOnlyCollection<Object> IContainer.Contents => _inventory;

    void IContainer.AddToContents(Object item) => _inventory.Add(item);

    void IContainer.RemoveFromContents(Object item) => _inventory.Remove(item);

    public bool IsChannelMuted(string channel) => _mutedChannels.Contains(channel);

    public bool MuteChannel(string channel) => _mutedChannels.Add(channel);

    public bool UnmuteChannel(string channel) => _mutedChannels.Remove(channel);

    public override string ToString() => Username;
}