using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using MooSharp.Actors;
using MooSharp.Messaging;
using MooSharp.Persistence.Dtos;

namespace MooSharp.Persistence;

public class QueuedPlayerStore(
    ChannelWriter<DatabaseRequest> writer,
    SqlitePlayerStore playerStore,
    ILogger<QueuedPlayerStore> logger) : IPlayerStore
{
    public Task SaveNewPlayer(Player player, Room currentLocation, string password, CancellationToken ct = default)
    {
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

        var dto = PlayerSnapshotFactory.CreateNewPlayerDto(player, currentLocation, hashedPassword);

        return EnqueueAsync(new SaveNewPlayerRequest(dto), ct);
    }

    public Task SavePlayer(Player player, Room currentLocation, CancellationToken ct = default)
    {
        var snapshot = PlayerSnapshotFactory.CreateSnapshot(player, currentLocation);

        return EnqueueAsync(new SavePlayerRequest(snapshot), ct);
    }

    public Task<PlayerDto?> LoadPlayer(LoginCommand command, CancellationToken ct = default)
    {
        return playerStore.LoadPlayer(command, ct);
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
