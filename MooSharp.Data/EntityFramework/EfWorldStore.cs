using Microsoft.EntityFrameworkCore;
using MooSharp.Data.Dtos;

namespace MooSharp.Data.EntityFramework;

internal sealed class EfWorldStore(IDbContextFactory<MooSharpDbContext> contextFactory) : IWorldStore
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
            .ToListAsync(cancellationToken);

        return rooms.Select(ToSnapshot).ToList();
    }

    public async Task SaveRoomAsync(RoomSnapshotDto room, CancellationToken cancellationToken = default)
    {
        await SaveRoomSnapshotsAsync([room], cancellationToken);
    }

    public async Task SaveExitAsync(string fromRoomId, string toRoomId, string direction, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var exit = new ExitEntity
        {
            FromRoomId = fromRoomId,
            ToRoomId = toRoomId
        };

        await context.Exits.AddAsync(exit, cancellationToken);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Ignore duplicate exits
        }
    }

    public async Task SaveRoomsAsync(IEnumerable<RoomSnapshotDto> rooms, CancellationToken cancellationToken = default)
    {
        await SaveRoomSnapshotsAsync(rooms, cancellationToken);
    }

    public async Task UpdateRoomDescriptionAsync(string roomId, string description, string longDescription,
        CancellationToken cancellationToken = default)
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

    public async Task RenameRoomAsync(string roomId, string name, CancellationToken cancellationToken = default)
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

    public async Task RenameObjectAsync(string objectId, string name, CancellationToken cancellationToken = default)
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

    private async Task SaveRoomSnapshotsAsync(IEnumerable<RoomSnapshotDto> rooms, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var roomList = rooms.ToList();

        foreach (var room in roomList)
        {
            var existing = await context.Rooms
                .Include(r => r.Exits)
                .Include(r => r.Objects)
                .FirstOrDefaultAsync(r => r.Id == room.Id, cancellationToken);

            if (existing is null)
            {
                existing = new RoomEntity { Id = room.Id };
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
                .Select(exit => new ExitEntity { FromRoomId = room.Id, ToRoomId = exit.Value })
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
                    CreatorUsername = o.CreatorUsername
                })
                .ToList();
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static RoomSnapshotDto ToSnapshot(RoomEntity entity)
    {
        var exits = entity.Exits.ToDictionary(e => e.ToRoomId, e => e.ToRoomId, StringComparer.OrdinalIgnoreCase);

        var objects = entity.Objects
            .Select(o => new ObjectSnapshotDto(
                o.Id,
                o.RoomId,
                o.Name,
                o.Description,
                o.TextContent,
                o.Flags,
                o.KeyId,
                o.CreatorUsername))
            .ToList();

        return new RoomSnapshotDto(
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
}
