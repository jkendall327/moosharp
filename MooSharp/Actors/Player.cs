namespace MooSharp;

public class Player
{
    public Guid Id { get; } = Guid.CreateVersion7();
    public required RoomActor CurrentLocation { get; set; }
    public Dictionary<string, ObjectActor> Inventory { get; } = new();
    public required string Username { get; init; }
}

public class PlayerActor(Player state) : Actor<Player>(state)
{
    public async Task<Dictionary<string, RoomActor>> GetCurrentlyAvailableExitsAsync()
    {
        var current = await QueryAsync(s => s.CurrentLocation);
        return await current.QueryAsync(s => s.Exits);
    }
}