using MooSharp.Data.Dtos;

namespace MooSharp.Data;

internal interface IPlayerStore
{
    Task SaveNewPlayerAsync(NewPlayerRequest player, CancellationToken ct);
    Task SavePlayerAsync(PlayerSnapshotDto snapshot, CancellationToken ct);
    Task<PlayerDto?> LoadPlayerAsync(Guid id, CancellationToken ct);
    Task<PlayerDto?> GetPlayerByUsernameAsync(string username, CancellationToken ct);
}