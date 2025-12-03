using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using MooSharp.Data.Dtos;
using MooSharp.Data.EntityFramework;

namespace MooSharp.Data.Queueing;

internal sealed class QueuedPlayerStore(
    ChannelWriter<DatabaseRequest> writer,
    EfPlayerStore playerStore,
    ILogger<QueuedPlayerStore> logger) : IPlayerStore
{
    public Task SaveNewPlayerAsync(NewPlayerRequest player, CancellationToken ct = default)
    {
        return EnqueueAsync(new SaveNewPlayerRequest(player), ct);
    }

    public Task SavePlayerAsync(PlayerSnapshotDto snapshot, CancellationToken ct = default)
    {
        return EnqueueAsync(new SavePlayerRequest(snapshot), ct);
    }

    public Task<PlayerDto?> LoadPlayerAsync(LoginRequest command, CancellationToken ct = default)
    {
        return playerStore.LoadPlayerAsync(command, ct);
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
