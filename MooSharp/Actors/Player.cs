using MooSharp.Messaging;

namespace MooSharp;

public readonly record struct PlayerId(Guid Value)
{
    public static PlayerId New() => new(Guid.CreateVersion7());
}

public class Player
{
    public PlayerId Id { get; } = PlayerId.New();
    public required IPlayerConnection Connection { get; init; }
    public required Room CurrentLocation { get; set; }
    public Dictionary<string, Object> Inventory { get; } = new();
    public required string Username { get; init; }
    public override string ToString() => Username;
}