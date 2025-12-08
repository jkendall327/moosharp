using Microsoft.AspNetCore.SignalR;
using MooSharp.Features.Editor;
using MooSharp.Web.Services.SignalR;

namespace MooSharp.Web.Services.Session;

public record EditorModeNotification(
    bool IsActive,
    string? ObjectName = null,
    string? VerbName = null,
    string? Instructions = null
);

public class SignalREditorModeNotifier(IHubContext<MooHub> hubContext) : IEditorModeNotifier
{
    public const string EditorModeChanged = "EditorModeChanged";

    public async Task NotifyEditorModeEnteredAsync(Guid playerId, string objectName, string verbName, CancellationToken ct = default)
    {
        var notification = new EditorModeNotification(
            IsActive: true,
            ObjectName: objectName,
            VerbName: verbName,
            Instructions: "Enter Lua code line by line. Type '.' on a blank line to save and exit, or '@abort' to cancel."
        );

        await hubContext.Clients.Group(playerId.ToString()).SendAsync(EditorModeChanged, notification, ct);
    }

    public async Task NotifyEditorModeExitedAsync(Guid playerId, CancellationToken ct = default)
    {
        var notification = new EditorModeNotification(IsActive: false);

        await hubContext.Clients.Group(playerId.ToString()).SendAsync(EditorModeChanged, notification, ct);
    }
}
