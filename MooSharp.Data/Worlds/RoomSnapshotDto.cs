namespace MooSharp.Data.Dtos;

public sealed record RoomSnapshotDto(
    string Id,
    string Name,
    string Description,
    string LongDescription,
    string EnterText,
    string ExitText,
    string? CreatorUsername,
    IReadOnlyDictionary<string, string> Exits,
    IReadOnlyCollection<ObjectSnapshotDto> Objects);

public sealed record ObjectSnapshotDto(
    string Id,
    string RoomId,
    string Name,
    string Description,
    string? TextContent,
    int Flags,
    string? KeyId,
    string? CreatorUsername);
