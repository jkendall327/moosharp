namespace MooSharp.Data.Players;

public interface IPlayerRepository
{
    Task SaveNewPlayerAsync(NewPlayerRequest player,
        WriteType type = WriteType.Deferred,
        CancellationToken ct = default);

    Task SavePlayerAsync(PlayerSnapshotDto snapshot,
        WriteType type = WriteType.Deferred,
        CancellationToken ct = default);

    Task<PlayerDto?> LoadPlayerAsync(Guid actorId, CancellationToken ct = default);

    Task<PlayerDto?> GetPlayerByUsername(string username, CancellationToken ct = default);

    Task UpdatePlayerDescriptionAsync(Guid playerId,
        string description,
        WriteType type = WriteType.Deferred,
        CancellationToken ct = default);
}