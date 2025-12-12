using MooSharp.Data;

namespace MooSharp.Data.Worlds;

public interface IWorldRepository
{
    Task<bool> HasRoomsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<RoomSnapshotDto>> LoadRoomsAsync(CancellationToken cancellationToken = default);

    Task SaveRoomAsync(RoomSnapshotDto room,
        WriteType type = WriteType.Deferred,
        CancellationToken cancellationToken = default);

    Task SaveExitAsync(string fromRoomId, ExitSnapshotDto exit,
        WriteType type = WriteType.Deferred,
        CancellationToken cancellationToken = default);

    Task SaveRoomsAsync(IEnumerable<RoomSnapshotDto> rooms,
        WriteType type = WriteType.Deferred,
        CancellationToken cancellationToken = default);

    Task UpdateRoomDescriptionAsync(string roomId, string description, string longDescription,
        WriteType type = WriteType.Deferred,
        CancellationToken cancellationToken = default);

    Task RenameRoomAsync(string roomId, string name,
        WriteType type = WriteType.Deferred,
        CancellationToken cancellationToken = default);

    Task RenameObjectAsync(string objectId, string name,
        WriteType type = WriteType.Deferred,
        CancellationToken cancellationToken = default);
}
