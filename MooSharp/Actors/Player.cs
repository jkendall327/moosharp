namespace MooSharp;

public class Player
{
    public Guid Id { get; } = Guid.CreateVersion7();
    public RoomActor? CurrentLocation { get; set; }
    public required string Username { get; init; }

    public async Task<Dictionary<string, RoomActor>> GetCurrentlyAvailableExitsAsync()
    {
        return CurrentLocation == null ? new() : await CurrentLocation.QueryAsync(s => s.Exits);
    }
}