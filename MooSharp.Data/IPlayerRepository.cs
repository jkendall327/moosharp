using MooSharp.Data.Dtos;

namespace MooSharp.Data;

public interface IPlayerRepository
{
    Task SaveNewPlayerAsync(NewPlayerRequest player,
        WriteType type = WriteType.Deferred,
        CancellationToken ct = default);

    Task SavePlayerAsync(PlayerSnapshotDto snapshot,
        WriteType type = WriteType.Deferred,
        CancellationToken ct = default);

    Task<PlayerDto?> LoadPlayerAsync(LoginRequest command, CancellationToken ct = default);
}