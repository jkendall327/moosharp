namespace MooSharp.World.Dtos;

public class WorldDto
{
    public List<RoomDto> Rooms { get; init; } = [];
    public List<ObjectDto> Objects { get; init; } = [];
}
