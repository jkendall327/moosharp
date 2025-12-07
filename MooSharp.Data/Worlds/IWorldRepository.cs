namespace MooSharp.Data.Worlds;

public interface IWorldRepository
{
    Task<bool> HasRoomsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<RoomSnapshotDto>> LoadRoomsAsync(CancellationToken cancellationToken = default);

    Task SaveRoomAsync(RoomSnapshotDto room, CancellationToken cancellationToken = default);

    Task SaveExitAsync(string fromRoomId, string toRoomId, string direction, CancellationToken cancellationToken = default);

    Task SaveRoomsAsync(IEnumerable<RoomSnapshotDto> rooms, CancellationToken cancellationToken = default);

    Task UpdateRoomDescriptionAsync(string roomId, string description, string longDescription,
        CancellationToken cancellationToken = default);

    Task RenameRoomAsync(string roomId, string name, CancellationToken cancellationToken = default);

    Task RenameObjectAsync(string objectId, string name, CancellationToken cancellationToken = default);
}
