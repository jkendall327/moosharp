using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MooSharp.Data.EntityFramework;

namespace MooSharp.Data.Worlds;

internal sealed class EfWorldRepository(ChannelWriter<DatabaseRequest> writer,
    IDbContextFactory<MooSharpDbContext> contextFactory,
    ILogger<EfWorldRepository> logger) : IWorldRepository
{
    public async Task<bool> HasRoomsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Rooms.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<RoomSnapshotDto>> LoadRoomsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var rooms = await context.Rooms
            .Include(r => r.Exits)
            .Include(r => r.Objects)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return rooms.Select(ToSnapshot).ToList();
    }

    public async Task SaveRoomAsync(RoomSnapshotDto room,
        WriteType type = WriteType.Deferred,
        CancellationToken cancellationToken = default)
    {
        if (type is WriteType.Deferred)
        {
            await EnqueueAsync(new SaveRoomRequest(room), cancellationToken);
        }
        else
        {
            await SaveRoomSnapshotsAsync([room], cancellationToken);
        }
    }

    public async Task SaveExitAsync(string fromRoomId, ExitSnapshotDto exitSnapshot,
        WriteType type = WriteType.Deferred,
        CancellationToken cancellationToken = default)
    {
        if (type is WriteType.Deferred)
        {
            await EnqueueAsync(new SaveExitRequest(fromRoomId, exitSnapshot), cancellationToken);
        }
        else
        {
            await SaveExitAsyncImmediate(fromRoomId, exitSnapshot, cancellationToken);
        }
    }

    public async Task SaveRoomsAsync(IEnumerable<RoomSnapshotDto> rooms,
        WriteType type = WriteType.Deferred,
        CancellationToken cancellationToken = default)
    {
        if (type is WriteType.Deferred)
        {
            await EnqueueAsync(new SaveRoomsRequest(rooms.ToList()), cancellationToken);
        }
        else
        {
            await SaveRoomSnapshotsAsync(rooms, cancellationToken);
        }
    }

    public async Task UpdateRoomDescriptionAsync(string roomId, string description, string longDescription,
        WriteType type = WriteType.Deferred,
        CancellationToken cancellationToken = default)
    {
        if (type is WriteType.Deferred)
        {
            await EnqueueAsync(new UpdateRoomDescriptionRequest(roomId, description, longDescription), cancellationToken);
        }
        else
        {
            await UpdateRoomDescriptionImmediateAsync(roomId, description, longDescription, cancellationToken);
        }
    }

    public async Task RenameRoomAsync(string roomId, string name,
        WriteType type = WriteType.Deferred,
        CancellationToken cancellationToken = default)
    {
        if (type is WriteType.Deferred)
        {
            await EnqueueAsync(new RenameRoomRequest(roomId, name), cancellationToken);
        }
        else
        {
            await RenameRoomImmediateAsync(roomId, name, cancellationToken);
        }
    }

    public async Task RenameObjectAsync(string objectId, string name,
        WriteType type = WriteType.Deferred,
        CancellationToken cancellationToken = default)
    {
        if (type is WriteType.Deferred)
        {
            await EnqueueAsync(new RenameObjectRequest(objectId, name), cancellationToken);
        }
        else
        {
            await RenameObjectImmediateAsync(objectId, name, cancellationToken);
        }
    }

    private async Task SaveRoomSnapshotsAsync(IEnumerable<RoomSnapshotDto> rooms, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var roomList = rooms.ToList();

        foreach (var room in roomList)
        {
            var existing = await context.Rooms
                .Include(r => r.Exits)
                .Include(r => r.Objects)
                .AsSplitQuery()
                .FirstOrDefaultAsync(r => r.Id == room.Id, cancellationToken);

            if (existing is null)
            {
                existing = new()
                { Id = room.Id };
                context.Rooms.Add(existing);
            }

            existing.Name = room.Name;
            existing.Description = room.Description;
            existing.LongDescription = room.LongDescription;
            existing.EnterText = room.EnterText;
            existing.ExitText = room.ExitText;
            existing.CreatorUsername = room.CreatorUsername;

            context.Exits.RemoveRange(existing.Exits);
            existing.Exits = room.Exits
                .Select(exit => new ExitEntity
                {
                    Id = exit.Id,
                    FromRoomId = room.Id,
                    DestinationRoomId = exit.DestinationRoomId,
                    Name = exit.Name,
                    Description = exit.Description,
                    IsHidden = exit.IsHidden,
                    IsLocked = exit.IsLocked,
                    IsOpen = exit.IsOpen,
                    CanBeOpened = exit.CanBeOpened,
                    CanBeLocked = exit.CanBeLocked,
                    KeyId = exit.KeyId,
                    Aliases = Serialize(exit.Aliases),
                    Keywords = Serialize(exit.Keywords)
                })
                .ToList();

            context.Objects.RemoveRange(existing.Objects);
            existing.Objects = room.Objects
                .Select(o => new ObjectEntity
                {
                    Id = o.Id,
                    RoomId = room.Id,
                    Name = o.Name,
                    Description = o.Description,
                    TextContent = o.TextContent,
                    Flags = o.Flags,
                    KeyId = o.KeyId,
                    CreatorUsername = o.CreatorUsername,
                    DynamicPropertiesJson = o.DynamicPropertiesJson,
                    VerbScriptsJson = o.VerbScriptsJson
                })
                .ToList();
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static RoomSnapshotDto ToSnapshot(RoomEntity entity)
    {
        var exits = entity.Exits
            .Select(e => new ExitSnapshotDto(
                e.Id,
                e.Name,
                e.Description,
                e.DestinationRoomId,
                e.IsHidden,
                e.IsLocked,
                e.IsOpen,
                e.CanBeOpened,
                e.CanBeLocked,
                e.KeyId,
                Deserialize(e.Aliases),
                Deserialize(e.Keywords)))
            .ToList();

        var objects = entity.Objects
            .Select(o => new ObjectSnapshotDto(
                o.Id,
                o.RoomId,
                o.Name,
                o.Description,
                o.TextContent,
                o.Flags,
                o.KeyId,
                o.CreatorUsername,
                o.DynamicPropertiesJson,
                o.VerbScriptsJson))
            .ToList();

        return new(
            entity.Id,
            entity.Name,
            entity.Description,
            entity.LongDescription,
            entity.EnterText,
            entity.ExitText,
            entity.CreatorUsername,
            exits,
            objects);
    }

    private static string Serialize(IEnumerable<string> values)
    {
        return string.Join('|', values);
    }

    private static IReadOnlyCollection<string> Deserialize(string data)
    {
        return string.IsNullOrWhiteSpace(data)
            ? []
            : data.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private async Task SaveExitAsyncImmediate(string fromRoomId, ExitSnapshotDto exitSnapshot, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var exitEntity = new ExitEntity
        {
            Id = exitSnapshot.Id,
            FromRoomId = fromRoomId,
            DestinationRoomId = exitSnapshot.DestinationRoomId,
            Name = exitSnapshot.Name,
            Description = exitSnapshot.Description,
            IsHidden = exitSnapshot.IsHidden,
            IsLocked = exitSnapshot.IsLocked,
            IsOpen = exitSnapshot.IsOpen,
            CanBeOpened = exitSnapshot.CanBeOpened,
            CanBeLocked = exitSnapshot.CanBeLocked,
            KeyId = exitSnapshot.KeyId,
            Aliases = Serialize(exitSnapshot.Aliases),
            Keywords = Serialize(exitSnapshot.Keywords)
        };

        await context.Exits.AddAsync(exitEntity, cancellationToken);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Ignore duplicate exits
        }
    }

    private async Task UpdateRoomDescriptionImmediateAsync(string roomId, string description, string longDescription,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var room = await context.Rooms.FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken);

        if (room is null)
        {
            return;
        }

        room.Description = description;
        room.LongDescription = longDescription;

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task RenameRoomImmediateAsync(string roomId, string name, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var room = await context.Rooms.FirstOrDefaultAsync(r => r.Id == roomId, cancellationToken);

        if (room is null)
        {
            return;
        }

        room.Name = name;

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task RenameObjectImmediateAsync(string objectId, string name, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var obj = await context.Objects.FirstOrDefaultAsync(o => o.Id == objectId, cancellationToken);

        if (obj is null)
        {
            return;
        }

        obj.Name = name;

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnqueueAsync(DatabaseRequest request, CancellationToken ct)
    {
        try
        {
            await writer.WriteAsync(request, ct);
        }
        catch (ChannelClosedException)
        {
            logger.LogWarning("Database request channel was closed; request {RequestType} was dropped", request.GetType().Name);
        }
    }
}
