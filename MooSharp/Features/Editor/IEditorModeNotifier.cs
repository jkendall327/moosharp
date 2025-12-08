namespace MooSharp.Features.Editor;

/// <summary>
/// Abstraction for notifying clients about editor mode changes.
/// Implemented in MooSharp.Web to use SignalR.
/// </summary>
public interface IEditorModeNotifier
{
    Task NotifyEditorModeEnteredAsync(Guid playerId, string objectName, string verbName, CancellationToken ct = default);

    Task NotifyEditorModeExitedAsync(Guid playerId, CancellationToken ct = default);
}
