using Microsoft.Extensions.Logging;

namespace MooSharp;

public class Player
{
    public Guid Id { get; } = Guid.CreateVersion7();
    public required string ConnectionId { get; init; }
    public required Room CurrentLocation { get; set; }
    public Dictionary<string, Object> Inventory { get; } = new();
    public required string Username { get; init; }
    public override string ToString() => Username;
}