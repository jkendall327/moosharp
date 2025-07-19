namespace MooSharp;

public class Player
{
    public RoomActor? CurrentLocation { get; set; }
    public required string Username { get; init; }
}