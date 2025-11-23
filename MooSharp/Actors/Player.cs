namespace MooSharp;

public readonly record struct PlayerId(Guid Value)
{
    public static PlayerId New() => new(Guid.CreateVersion7());
}

public readonly record struct ConnectionId(string Value)
{
    public override string ToString() => Value;
    public static implicit operator ConnectionId(string value) => new(value);
}

public class Player
{
    public PlayerId Id { get; } = PlayerId.New();
    public required ConnectionId ConnectionId { get; init; }
    public required Room CurrentLocation { get; set; }
    public Dictionary<string, Object> Inventory { get; } = new();
    public required string Username { get; init; }
    public override string ToString() => Username;
}