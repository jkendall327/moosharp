using MooSharp.Actors.Objects;
using MooSharp.Actors.Rooms;

namespace MooSharp.World;

public class WorldDto
{
    public List<RoomDto> Rooms { get; init; } = [];
    public List<ObjectDto> Objects { get; init; } = [];
}
