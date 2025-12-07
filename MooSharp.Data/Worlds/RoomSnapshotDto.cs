using System;
using System.Collections.Generic;

namespace MooSharp.Data.Worlds;

public sealed record RoomSnapshotDto(
    string Id,
    string Name,
    string Description,
    string LongDescription,
    string EnterText,
    string ExitText,
    string? CreatorUsername,
    IReadOnlyCollection<ExitSnapshotDto> Exits,
    IReadOnlyCollection<ObjectSnapshotDto> Objects);

public sealed record ObjectSnapshotDto(
    string Id,
    string RoomId,
    string Name,
    string Description,
    string? TextContent,
    int Flags,
    string? KeyId,
    string? CreatorUsername,
    string? DynamicPropertiesJson = null,
    string? VerbScriptsJson = null);

public sealed record ExitSnapshotDto(
    Guid Id,
    string Name,
    string Description,
    string DestinationRoomId,
    bool IsHidden,
    bool IsLocked,
    bool IsOpen,
    bool CanBeOpened,
    bool CanBeLocked,
    string? KeyId,
    IReadOnlyCollection<string> Aliases,
    IReadOnlyCollection<string> Keywords);
