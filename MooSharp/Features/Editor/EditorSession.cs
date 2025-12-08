using MooSharp.Actors.Objects;

namespace MooSharp.Features.Editor;

/// <summary>
/// Represents an active editor session for a player.
/// Tracks what verb they're editing and buffers their input lines.
/// </summary>
public class EditorSession
{
    public required Guid PlayerId { get; init; }
    public required ObjectId TargetObjectId { get; init; }
    public required string TargetObjectName { get; init; }
    public required string VerbName { get; init; }
    public List<string> BufferedLines { get; } = [];
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;

    public string GetAccumulatedCode() => string.Join("\n", BufferedLines);
}
