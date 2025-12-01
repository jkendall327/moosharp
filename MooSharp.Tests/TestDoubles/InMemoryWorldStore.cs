using MooSharp;
using MooSharp.Persistence;

namespace MooSharp.Tests.TestDoubles;

public sealed class InMemoryWorldStore : IWorldStore
{
    private readonly List<Room> _rooms = new();
    private readonly List<(RoomId From, RoomId To, string Direction)> _exits = new();

    public Task<bool> HasRoomsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_rooms.Any());

    public Task<IReadOnlyCollection<Room>> LoadRoomsAsync(CancellationToken cancellationToken = default)
    {
        var rooms = _rooms.Select(CloneRoom).ToList();

        foreach (var exit in _exits)
        {
            var origin = rooms.SingleOrDefault(r => r.Id == exit.From);
            if (origin is null)
            {
                continue;
            }

            origin.Exits[exit.Direction] = exit.To;
        }

        return Task.FromResult<IReadOnlyCollection<Room>>(rooms);
    }

    public Task SaveRoomAsync(Room room, CancellationToken cancellationToken = default)
    {
        _rooms.RemoveAll(r => r.Id == room.Id);
        _rooms.Add(CloneRoom(room));
        return Task.CompletedTask;
    }

    public Task SaveExitAsync(RoomId fromRoomId, RoomId toRoomId, string direction,
        CancellationToken cancellationToken = default)
    {
        _exits.RemoveAll(e => e.From == fromRoomId && string.Equals(e.Direction, direction, StringComparison.OrdinalIgnoreCase));
        _exits.Add((fromRoomId, toRoomId, direction));
        return Task.CompletedTask;
    }

    public Task SaveRoomsAsync(IEnumerable<Room> rooms, CancellationToken cancellationToken = default)
    {
        _rooms.Clear();
        _rooms.AddRange(rooms.Select(CloneRoom));

        _exits.Clear();

        foreach (var room in rooms)
        {
            foreach (var exit in room.Exits)
            {
                _exits.Add((room.Id, exit.Value, exit.Key));
            }
        }

        return Task.CompletedTask;
    }

    public Task UpdateRoomDescriptionAsync(RoomId roomId, string description, string longDescription,
        CancellationToken cancellationToken = default)
    {
        var existing = _rooms.SingleOrDefault(r => r.Id == roomId);

        if (existing is null)
        {
            return Task.CompletedTask;
        }

        existing.Description = description;
        existing.LongDescription = longDescription;

        return Task.CompletedTask;
    }

    public Task RenameRoomAsync(RoomId roomId, string name, CancellationToken cancellationToken = default)
    {
        var existing = _rooms.SingleOrDefault(r => r.Id == roomId);

        if (existing is null)
        {
            return Task.CompletedTask;
        }

        existing.Name = name;

        return Task.CompletedTask;
    }

    public Task RenameObjectAsync(ObjectId objectId, string name, CancellationToken cancellationToken = default)
    {
        var existing = _rooms
            .SelectMany(r => r.Contents)
            .SingleOrDefault(o => o.Id == objectId);

        if (existing is not null)
        {
            existing.Name = name;
        }

        return Task.CompletedTask;
    }

    private static Room CloneRoom(Room room)
    {
        var clone = new Room
        {
            Id = room.Id,
            Name = room.Name,
            Description = room.Description,
            LongDescription = room.LongDescription,
            EnterText = room.EnterText,
            ExitText = room.ExitText,
            CreatorUsername = room.CreatorUsername
        };

        foreach (var exit in room.Exits)
        {
            clone.Exits[exit.Key] = exit.Value;
        }

        foreach (var item in room.Contents)
        {
            var clonedItem = new Object
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                Flags = item.Flags,
                KeyId = item.KeyId,
                CreatorUsername = item.CreatorUsername
            };

            if (!string.IsNullOrWhiteSpace(item.TextContent))
            {
                clonedItem.WriteText(item.TextContent);
            }

            clonedItem.MoveTo(clone);
        }

        return clone;
    }
}
