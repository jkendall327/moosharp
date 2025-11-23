namespace MooSharp;

public readonly record struct ConnectionId(string Value)
{
    public override string ToString() => Value;
    public static implicit operator ConnectionId(string value) => new(value);
}