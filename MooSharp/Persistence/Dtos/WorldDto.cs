namespace MooSharp;

public class WorldDto
{
    public List<RoomDto> Rooms { get; init; } = [];
    public List<ObjectDto> Objects { get; init; } = [];
}