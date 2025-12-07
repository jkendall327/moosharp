using MooSharp.Actors.Objects;
using MooSharp.Actors.Rooms;
using MooSharp.Data.Worlds;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.World;

public static class WorldSnapshotFactory
{
    public static RoomSnapshotDto CreateSnapshot(Room room)
    {
        ArgumentNullException.ThrowIfNull(room);

        var exits = room.Exits
            .Select(e => new ExitSnapshotDto(
                e.Id,
                e.Name,
                e.Description,
                e.Destination.Value,
                e.IsHidden,
                e.IsLocked,
                e.IsOpen,
                e.CanBeOpened,
                e.CanBeLocked,
                e.KeyId,
                e.Aliases.ToList(),
                e.Keywords.ToList()))
            .ToList();

        var objects = room.Contents
            .Select(o => new ObjectSnapshotDto(
                o.Id.Value.ToString(),
                room.Id.Value,
                o.Name,
                o.Description,
                o.TextContent,
                (int)o.Flags,
                o.KeyId,
                o.CreatorUsername,
                o.Properties.ToJson(),
                o.Verbs.ToJson()))
            .ToList();

        return new(
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
                Id = new(r.Id),
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
                room.Exits.Add(new()
                {
                    Id = exit.Id,
                    Name = exit.Name,
                    Description = exit.Description,
                    Destination = new(exit.DestinationRoomId),
                    Aliases = exit.Aliases.ToList(),
                    Keywords = exit.Keywords.ToList(),
                    IsHidden = exit.IsHidden,
                    IsLocked = exit.IsLocked,
                    IsOpen = exit.IsLocked ? false : exit.IsOpen,
                    CanBeOpened = exit.CanBeOpened,
                    CanBeLocked = exit.CanBeLocked,
                    KeyId = exit.KeyId
                });
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
                    Id = new(Guid.Parse(obj.Id)),
                    Name = obj.Name,
                    Description = obj.Description,
                    Flags = (ObjectFlags)obj.Flags,
                    KeyId = obj.KeyId,
                    CreatorUsername = obj.CreatorUsername,
                    Properties = DynamicPropertyBag.FromJson(obj.DynamicPropertiesJson),
                    Verbs = VerbCollection.FromJson(obj.VerbScriptsJson)
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
