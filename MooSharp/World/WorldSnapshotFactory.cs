using MooSharp.Actors;
using MooSharp.Data.Dtos;
using Object = MooSharp.Actors.Object;

namespace MooSharp.Data.Mapping;

public static class WorldSnapshotFactory
{
    public static RoomSnapshotDto CreateSnapshot(Room room)
    {
        ArgumentNullException.ThrowIfNull(room);

        var exits = new Dictionary<string, string>(room.Exits.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var exit in room.Exits)
        {
            exits[exit.Key] = exit.Value.Value;
        }

        var objects = room.Contents
            .Select(o => new ObjectSnapshotDto(
                o.Id.Value.ToString(),
                room.Id.Value,
                o.Name,
                o.Description,
                o.TextContent,
                (int)o.Flags,
                o.KeyId,
                o.CreatorUsername))
            .ToList();

        return new RoomSnapshotDto(
            room.Id.Value,
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

    public static IReadOnlyCollection<Room> CreateRooms(IEnumerable<RoomSnapshotDto> snapshots)
    {
        var rooms = snapshots.ToDictionary(
            r => r.Id,
            r => new Room
            {
                Id = new RoomId(r.Id),
                Name = r.Name,
                Description = r.Description,
                LongDescription = r.LongDescription,
                EnterText = r.EnterText,
                ExitText = r.ExitText,
                CreatorUsername = r.CreatorUsername
            });

        foreach (var roomSnapshot in snapshots)
        {
            var room = rooms[roomSnapshot.Id];

            foreach (var exit in roomSnapshot.Exits)
            {
                room.Exits[exit.Key] = new RoomId(exit.Value);
            }
        }

        foreach (var roomSnapshot in snapshots)
        {
            if (!rooms.TryGetValue(roomSnapshot.Id, out var room))
            {
                continue;
            }

            foreach (var obj in roomSnapshot.Objects)
            {
                var item = new Object
                {
                    Id = new ObjectId(Guid.Parse(obj.Id)),
                    Name = obj.Name,
                    Description = obj.Description,
                    Flags = (ObjectFlags)obj.Flags,
                    KeyId = obj.KeyId,
                    CreatorUsername = obj.CreatorUsername
                };

                if (!string.IsNullOrWhiteSpace(obj.TextContent))
                {
                    item.WriteText(obj.TextContent);
                }

                item.MoveTo(room);
            }
        }

        return rooms.Values.ToList();
    }
}
