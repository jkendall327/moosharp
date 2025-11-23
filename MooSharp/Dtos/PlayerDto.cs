namespace MooSharp.Persistence;

public class PlayerDto
{
    public RoomId CurrentLocation { get; set; }
    public required string Username { get; set; }
}