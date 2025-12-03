using MooSharp.Actors;
using MooSharp.Persistence.Dtos;

namespace MooSharp.Persistence;

public static class WorldSnapshotFactory
{
    public static RoomSnapshotDto CreateSnapshot(Room room)
    {
        ArgumentNullException.ThrowIfNull(room);

        var exits = new Dictionary<string, RoomId>(room.Exits, StringComparer.OrdinalIgnoreCase);

        var objects = room.Contents
            .Select(o => new ObjectSnapshotDto(
                o.Id,
                room.Id,
                o.Name,
                o.Description,
                o.TextContent,
                o.Flags,
                o.KeyId,
                o.CreatorUsername))
            .ToList();

        return new RoomSnapshotDto(
            room.Id,
            room.Name,
            room.Description,
            room.LongDescription,
            room.EnterText,
            room.ExitText,
            room.CreatorUsername,
            exits,
            objects);
    }

    public static IReadOnlyCollection<RoomSnapshotDto> CreateSnapshots(IEnumerable<Room> rooms)
    {
        ArgumentNullException.ThrowIfNull(rooms);

        return rooms.Select(CreateSnapshot).ToList();
    }
}
