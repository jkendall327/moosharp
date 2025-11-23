using Microsoft.Extensions.Logging;

namespace MooSharp;

public class Room
{
    public int Id { get; init; }
    public required string Name { get; init; } 
    public required string Slug { get; init; }
    public required string Description { get; init; } 
    public Dictionary<string, Object> Contents { get; } = new();
    public Dictionary<string, Room> Exits { get; } = new();
    public List<Player> PlayersInRoom { get; } = new();

    public override string ToString() => Slug;
}