using MooSharp.Data.Dtos;

namespace MooSharp.Data;

internal interface IPlayerStore
{
    Task SaveNewPlayerAsync(NewPlayerRequest player, CancellationToken ct);
    Task SavePlayerAsync(PlayerSnapshotDto snapshot, CancellationToken ct);
    Task<PlayerDto?> LoadPlayerAsync(string username, CancellationToken ct);
    Task<bool> PlayerWithUsernameExistsAsync(string username, CancellationToken ct);
}