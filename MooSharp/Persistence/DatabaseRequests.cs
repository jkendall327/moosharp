using MooSharp.Actors;
using MooSharp.Persistence.Dtos;

namespace MooSharp.Persistence;

public abstract record DatabaseRequest;

public sealed record SaveNewPlayerRequest(PlayerDto Player) : DatabaseRequest;

public sealed record SavePlayerRequest(PlayerSnapshotDto Snapshot) : DatabaseRequest;

public sealed record SaveRoomRequest(RoomSnapshotDto Room) : DatabaseRequest;

public sealed record SaveRoomsRequest(IReadOnlyCollection<RoomSnapshotDto> Rooms) : DatabaseRequest;

public sealed record SaveExitRequest(RoomId FromRoomId, RoomId ToRoomId) : DatabaseRequest;

public sealed record UpdateRoomDescriptionRequest(RoomId RoomId, string Description, string LongDescription) : DatabaseRequest;

public sealed record RenameRoomRequest(RoomId RoomId, string Name) : DatabaseRequest;

public sealed record RenameObjectRequest(ObjectId ObjectId, string Name) : DatabaseRequest;
