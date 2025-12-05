using MooSharp.Data.Dtos;

namespace MooSharp.Data;

public abstract record DatabaseRequest;

public sealed record SaveNewPlayerRequest(NewPlayerRequest Player) : DatabaseRequest;

public sealed record SavePlayerRequest(PlayerSnapshotDto Snapshot) : DatabaseRequest;

public sealed record SaveRoomRequest(RoomSnapshotDto Room) : DatabaseRequest;

public sealed record SaveRoomsRequest(IReadOnlyCollection<RoomSnapshotDto> Rooms) : DatabaseRequest;

public sealed record SaveExitRequest(string FromRoomId, string ToRoomId) : DatabaseRequest;

public sealed record UpdateRoomDescriptionRequest(string RoomId, string Description, string LongDescription) : DatabaseRequest;

public sealed record RenameRoomRequest(string RoomId, string Name) : DatabaseRequest;

public sealed record RenameObjectRequest(string ObjectId, string Name) : DatabaseRequest;
