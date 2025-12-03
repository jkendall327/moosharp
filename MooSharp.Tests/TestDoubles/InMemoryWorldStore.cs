using MooSharp.Data;
using MooSharp.Data.Dtos;

namespace MooSharp.Tests.TestDoubles;

public sealed class InMemoryWorldStore : IWorldStore
{
    private readonly List<RoomSnapshotDto> _rooms = [];

    public Task<bool> HasRoomsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_rooms.Any());

    public Task<IReadOnlyCollection<RoomSnapshotDto>> LoadRoomsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<RoomSnapshotDto>>(_rooms.Select(Clone).ToList());
    }

    public Task SaveRoomAsync(RoomSnapshotDto room, CancellationToken cancellationToken = default)
    {
        UpsertRoom(room);
        return Task.CompletedTask;
    }

    public Task SaveExitAsync(string fromRoomId, string toRoomId, string direction, CancellationToken cancellationToken = default)
    {
        var existing = _rooms.FirstOrDefault(r => r.Id == fromRoomId);

        if (existing is null)
        {
            return Task.CompletedTask;
        }

        var exits = new Dictionary<string, string>(existing.Exits, StringComparer.OrdinalIgnoreCase)
        {
            [direction] = toRoomId
        };

        ReplaceRoom(existing, existing with { Exits = exits });
        return Task.CompletedTask;
    }

    public Task SaveRoomsAsync(IEnumerable<RoomSnapshotDto> rooms, CancellationToken cancellationToken = default)
    {
        _rooms.Clear();
        _rooms.AddRange(rooms.Select(Clone));
        return Task.CompletedTask;
    }

    public Task UpdateRoomDescriptionAsync(string roomId, string description, string longDescription,
        CancellationToken cancellationToken = default)
    {
        var existing = _rooms.FirstOrDefault(r => r.Id == roomId);

        if (existing is not null)
        {
            ReplaceRoom(existing, existing with { Description = description, LongDescription = longDescription });
        }

        return Task.CompletedTask;
    }

    public Task RenameRoomAsync(string roomId, string name, CancellationToken cancellationToken = default)
    {
        var existing = _rooms.FirstOrDefault(r => r.Id == roomId);

        if (existing is not null)
        {
            ReplaceRoom(existing, existing with { Name = name });
        }

        return Task.CompletedTask;
    }

    public Task RenameObjectAsync(string objectId, string name, CancellationToken cancellationToken = default)
    {
        for (var i = 0; i < _rooms.Count; i++)
        {
            var room = _rooms[i];
            var objects = room.Objects.ToList();
            var index = objects.FindIndex(o => o.Id == objectId);

            if (index < 0)
            {
                continue;
            }

            var updated = objects[index] with { Name = name };
            objects[index] = updated;
            _rooms[i] = room with { Objects = objects };
            break;
        }

        return Task.CompletedTask;
    }

    private void UpsertRoom(RoomSnapshotDto room)
    {
        var existingIndex = _rooms.FindIndex(r => r.Id == room.Id);
        var clone = Clone(room);

        if (existingIndex >= 0)
        {
            _rooms[existingIndex] = clone;
        }
        else
        {
            _rooms.Add(clone);
        }
    }

    private void ReplaceRoom(RoomSnapshotDto original, RoomSnapshotDto replacement)
    {
        var index = _rooms.FindIndex(r => r.Id == original.Id);

        if (index >= 0)
        {
            _rooms[index] = Clone(replacement);
        }
    }

    private static RoomSnapshotDto Clone(RoomSnapshotDto room)
    {
        var exits = new Dictionary<string, string>(room.Exits, StringComparer.OrdinalIgnoreCase);
        var objects = room.Objects
            .Select(o => o with { })
            .ToList();

        return room with { Exits = exits, Objects = objects };
    }
}
