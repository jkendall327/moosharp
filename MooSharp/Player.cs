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
}