namespace MooSharp;

public class Player
{
    public Guid Id { get; } = Guid.CreateVersion7();
    public required RoomActor CurrentLocation { get; set; }
    public Dictionary<string, ObjectActor> Inventory { get; } = new();

    public required string Username { get; init; }

    public async Task<Dictionary<string, RoomActor>> GetCurrentlyAvailableExitsAsync()
    {
        return await CurrentLocation.QueryAsync(s => s.Exits);
    }
}