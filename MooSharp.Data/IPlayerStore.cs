using MooSharp.Data.Dtos;

namespace MooSharp.Data;

public interface IPlayerStore
{
    Task SaveNewPlayerAsync(NewPlayerRequest player, CancellationToken ct = default);

    Task SavePlayerAsync(PlayerSnapshotDto snapshot, CancellationToken ct = default);

    Task<PlayerDto?> LoadPlayerAsync(LoginRequest command, CancellationToken ct = default);
}
