using MooSharp.Actors;

namespace MooSharp.Actors.Rooms;

public class Exit : IOpenable, ILockable
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; set; }
    public List<string> Aliases { get; set; } = [];
    public List<string> Keywords { get; set; } = [];

    public required string Description { get; set; }
    public required RoomId Destination { get; set; }

    public bool IsHidden { get; set; }
    public bool IsLocked { get; set; }
    public bool IsOpen { get; set; } = true;
    public string? KeyId { get; set; }

    public bool CanBeOpened { get; set; } = true;
    public bool CanBeLocked { get; set; }
}
