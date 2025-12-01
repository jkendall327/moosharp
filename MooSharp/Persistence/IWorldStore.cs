namespace MooSharp.Persistence;

public interface IWorldStore
{
    Task<bool> HasRoomsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Room>> LoadRoomsAsync(CancellationToken cancellationToken = default);

    Task SaveRoomAsync(Room room, CancellationToken cancellationToken = default);

    Task SaveExitAsync(RoomId fromRoomId, RoomId toRoomId, string direction, CancellationToken cancellationToken = default);

    Task SaveRoomsAsync(IEnumerable<Room> rooms, CancellationToken cancellationToken = default);

    Task UpdateRoomDescriptionAsync(RoomId roomId, string description, string longDescription,
        CancellationToken cancellationToken = default);

    Task RenameRoomAsync(RoomId roomId, string name, CancellationToken cancellationToken = default);

    Task RenameObjectAsync(ObjectId objectId, string name, CancellationToken cancellationToken = default);
}
