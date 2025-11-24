using MooSharp.Messaging;

namespace MooSharp;

public readonly record struct PlayerId(Guid Value)
{
    public static PlayerId New() => new(Guid.CreateVersion7());
}

public class Player
{
    public PlayerId Id { get; } = PlayerId.New();
    public required IPlayerConnection Connection { get; set; }
    public List<Object> Inventory { get; } = [];
    public required string Username { get; init; }
    public override string ToString() => Username;
}