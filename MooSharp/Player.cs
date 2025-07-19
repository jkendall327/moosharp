namespace MooSharp;

public class Player
{
    public Guid Id { get; } = Guid.CreateVersion7();
    public RoomActor? CurrentLocation { get; set; }
    public required string Username { get; init; }

    public Dictionary<string, RoomActor> GetCurrentlyAvailableExits()
    {
        return CurrentLocation == null ? new() : CurrentLocation.QueryState(s => s.Exits);
    }

    public override bool Equals(object? obj)
    {
        return obj is Player player && string.Equals(player.Username, Username, StringComparison.Ordinal);
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Username);
    }
}