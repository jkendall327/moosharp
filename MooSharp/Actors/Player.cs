using Microsoft.Extensions.Logging;

namespace MooSharp;

public class Player
{
    public Guid Id { get; } = Guid.CreateVersion7();
    public required RoomActor CurrentLocation { get; set; }
    public Dictionary<string, ObjectActor> Inventory { get; } = new();
    public required string Username { get; init; }
    public override string ToString() => Username;
}

public class PlayerActor(Player state, ILoggerFactory factory) : Actor<Player>(state, factory)
{
    public string Username => State.Username;
    
    public async Task<IReadOnlyDictionary<string, RoomActor>> GetCurrentlyAvailableExitsAsync()
    {
        var current = await QueryAsync(s => s.CurrentLocation);
        
        return current.Exits;
    }
}