using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using MooSharp.Actors;
using MooSharp.Persistence.Dtos;

namespace MooSharp.Persistence;

public class QueuedWorldStore(
    ChannelWriter<DatabaseRequest> writer,
    SqliteWorldStore worldStore,
    ILogger<QueuedWorldStore> logger) : IWorldStore
{
    public Task<bool> HasRoomsAsync(CancellationToken cancellationToken = default)
    {
        return worldStore.HasRoomsAsync(cancellationToken);
    }

    public Task<IReadOnlyCollection<Room>> LoadRoomsAsync(CancellationToken cancellationToken = default)
    {
        return worldStore.LoadRoomsAsync(cancellationToken);
    }

    public Task SaveRoomAsync(Room room, CancellationToken cancellationToken = default)
    {
        var snapshot = WorldSnapshotFactory.CreateSnapshot(room);

        return EnqueueAsync(new SaveRoomRequest(snapshot), cancellationToken);
    }

    public Task SaveExitAsync(RoomId fromRoomId, RoomId toRoomId, string direction, CancellationToken cancellationToken = default)
    {
        _ = direction;

        return EnqueueAsync(new SaveExitRequest(fromRoomId, toRoomId), cancellationToken);
    }

    public Task SaveRoomsAsync(IEnumerable<Room> rooms, CancellationToken cancellationToken = default)
    {
        var snapshots = WorldSnapshotFactory.CreateSnapshots(rooms);

        return EnqueueAsync(new SaveRoomsRequest(snapshots), cancellationToken);
    }

    public Task UpdateRoomDescriptionAsync(RoomId roomId, string description, string longDescription,
        CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(new UpdateRoomDescriptionRequest(roomId, description, longDescription), cancellationToken);
    }

    public Task RenameRoomAsync(RoomId roomId, string name, CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(new RenameRoomRequest(roomId, name), cancellationToken);
    }

    public Task RenameObjectAsync(ObjectId objectId, string name, CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(new RenameObjectRequest(objectId, name), cancellationToken);
    }

    private async Task EnqueueAsync(DatabaseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await writer.WriteAsync(request, cancellationToken);
        }
        catch (ChannelClosedException)
        {
            logger.LogWarning("Database request channel was closed; request {RequestType} was dropped", request.GetType().Name);
        }
    }
}
