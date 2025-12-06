using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using MooSharp.Data.Dtos;

namespace MooSharp.Data.EntityFramework;

internal sealed class EfPlayerRepository(
    ChannelWriter<DatabaseRequest> writer,
    IPlayerStore store,
    ILogger<EfPlayerRepository> logger) : IPlayerRepository
{
    public async Task SaveNewPlayerAsync(NewPlayerRequest player,
        WriteType type = WriteType.Deferred,
        CancellationToken ct = default)
    {
        if (type is WriteType.Deferred)
        {
            await writer.WriteAsync(new SaveNewPlayerRequest(player), ct);
        }
        else
        {
            await store.SaveNewPlayerAsync(player, ct);
        }
    }

    public async Task SavePlayerAsync(PlayerSnapshotDto snapshot,
        WriteType type = WriteType.Deferred,
        CancellationToken ct = default)
    {
        if (type is WriteType.Deferred)
        {
            await EnqueueAsync(new SavePlayerRequest(snapshot), ct);
        }
        else
        {
            await store.SavePlayerAsync(snapshot, ct);
        }
    }

    public Task<PlayerDto?> LoadPlayerAsync(Guid id, CancellationToken ct) =>
        store.LoadPlayerAsync(id, ct);

    public Task<bool> PlayerWithUsernameExistsAsync(string username, CancellationToken ct = default) =>
        store.PlayerWithUsernameExistsAsync(username, ct);

    private async Task EnqueueAsync(DatabaseRequest request, CancellationToken ct)
    {
        try
        {
            await writer.WriteAsync(request, ct);
        }
        catch (ChannelClosedException)
        {
            var name = request.GetType()
                .Name;

            logger.LogWarning("Database request channel was closed; request {RequestType} was dropped", name);
        }
    }
}