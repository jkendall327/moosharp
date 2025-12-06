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

    Task<PlayerDto?> LoadPlayerAsync(string username, CancellationToken ct = default);
    
    Task<bool> PlayerWithUsernameExistsAsync(string username, CancellationToken ct = default);
}