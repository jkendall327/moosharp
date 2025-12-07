using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using MooSharp.Data.Dtos;
using MooSharp.Data.EntityFramework;

namespace MooSharp.Data.Queueing;

internal sealed class QueuedWorldRepository(
    ChannelWriter<DatabaseRequest> writer,
    EfWorldRepository worldRepository,
    ILogger<QueuedWorldRepository> logger) : IWorldRepository
{
    public async Task<bool> HasRoomsAsync(CancellationToken cancellationToken = default)
    {
        return await worldRepository.HasRoomsAsync(cancellationToken);
    }

    public Task<IReadOnlyCollection<RoomSnapshotDto>> LoadRoomsAsync(CancellationToken cancellationToken = default)
    {
        return worldRepository.LoadRoomsAsync(cancellationToken);
    }

    public Task SaveRoomAsync(RoomSnapshotDto room, CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(new SaveRoomRequest(room), cancellationToken);
    }

    public Task SaveExitAsync(string fromRoomId, string toRoomId, string direction, CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(new SaveExitRequest(fromRoomId, toRoomId), cancellationToken);
    }

    public Task SaveRoomsAsync(IEnumerable<RoomSnapshotDto> rooms, CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(new SaveRoomsRequest(rooms.ToList()), cancellationToken);
    }

    public Task UpdateRoomDescriptionAsync(string roomId, string description, string longDescription,
        CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(new UpdateRoomDescriptionRequest(roomId, description, longDescription), cancellationToken);
    }

    public Task RenameRoomAsync(string roomId, string name, CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(new RenameRoomRequest(roomId, name), cancellationToken);
    }

    public Task RenameObjectAsync(string objectId, string name, CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(new RenameObjectRequest(objectId, name), cancellationToken);
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
