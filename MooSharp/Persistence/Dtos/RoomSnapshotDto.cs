using MooSharp.Actors;

namespace MooSharp.Persistence.Dtos;

public sealed record RoomSnapshotDto(
    RoomId Id,
    string Name,
    string Description,
    string LongDescription,
    string EnterText,
    string ExitText,
    string? CreatorUsername,
    IReadOnlyDictionary<string, RoomId> Exits,
    IReadOnlyCollection<ObjectSnapshotDto> Objects);

public sealed record ObjectSnapshotDto(
    ObjectId Id,
    RoomId RoomId,
    string Name,
    string Description,
    string? TextContent,
    ObjectFlags Flags,
    string? KeyId,
    string? CreatorUsername);
