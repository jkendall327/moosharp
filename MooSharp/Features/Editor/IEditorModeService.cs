using MooSharp.Actors.Objects;

namespace MooSharp.Features.Editor;

/// <summary>
/// Manages editor mode sessions for players.
/// </summary>
public interface IEditorModeService
{
    /// <summary>
    /// Start an editor session for a player.
    /// </summary>
    void StartSession(Guid playerId, ObjectId targetObjectId, string targetObjectName, string verbName);

    /// <summary>
    /// Check if a player is currently in editor mode.
    /// </summary>
    bool IsInEditorMode(Guid playerId);

    /// <summary>
    /// Get the current editor session for a player, or null if not in editor mode.
    /// </summary>
    EditorSession? GetSession(Guid playerId);

    /// <summary>
    /// Add a line to the buffer.
    /// </summary>
    void AddLine(Guid playerId, string line);

    /// <summary>
    /// End the session and return the accumulated session data.
    /// </summary>
    EditorSession? EndSession(Guid playerId);

    /// <summary>
    /// Cancel/abort the editor session without saving.
    /// </summary>
    void CancelSession(Guid playerId);
}
