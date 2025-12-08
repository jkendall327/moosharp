using MooSharp.Actors.Objects;
using MooSharp.Actors.Players;
using MooSharp.Commands.Presentation;
using MooSharp.Infrastructure.Messaging;

namespace MooSharp.Features.Editor;

public class EditorModeHandler(
    IEditorModeService editorService,
    World.World world,
    IGameMessageEmitter emitter,
    IEditorModeNotifier notifier) : IEditorModeHandler
{
    private const string AbortCommand = "@abort";
    private const string Terminator = ".";

    public async Task HandleEditorInputAsync(Player player, string input, CancellationToken ct = default)
    {
        var playerId = player.Id.Value;
        var session = editorService.GetSession(playerId);

        if (session is null)
        {
            return;
        }

        // Check for abort command
        if (string.Equals(input.Trim(), AbortCommand, StringComparison.OrdinalIgnoreCase))
        {
            editorService.CancelSession(playerId);

            var cancelMsg = new GameMessage(player, new EditorModeCancelledEvent(session.TargetObjectName, session.VerbName));
            await emitter.SendGameMessagesAsync([cancelMsg], ct);
            await notifier.NotifyEditorModeExitedAsync(playerId, ct);

            return;
        }

        // Check for terminator
        if (input.Trim() == Terminator)
        {
            await SaveAndExitAsync(player, session, ct);
            return;
        }

        // Buffer the line
        editorService.AddLine(playerId, input);

        // Echo line with number
        var lineNum = session.BufferedLines.Count;
        var echoMsg = new GameMessage(player, new EditorLineEchoEvent(lineNum, input));
        await emitter.SendGameMessagesAsync([echoMsg], ct);
    }

    private async Task SaveAndExitAsync(Player player, EditorSession session, CancellationToken ct)
    {
        var playerId = player.Id.Value;
        var endedSession = editorService.EndSession(playerId);

        if (endedSession is null)
        {
            return;
        }

        var code = endedSession.GetAccumulatedCode();

        // Find the object
        var room = world.GetLocationOrThrow(player);
        var target = FindObject(player, room, endedSession.TargetObjectId);

        if (target is null)
        {
            var errorMsg = new GameMessage(player,
                new SystemMessageEvent("The object you were editing no longer exists."));
            await emitter.SendGameMessagesAsync([errorMsg], ct);
            await notifier.NotifyEditorModeExitedAsync(playerId, ct);
            return;
        }

        // Update the verb
        var verbScript = VerbScript.Create(endedSession.VerbName, code, player.Username);
        target.Verbs[endedSession.VerbName] = verbScript;

        // Mark for persistence
        if (target.Location is not null)
        {
            world.MarkRoomModified(target.Location);
        }

        // Notify player
        var successMsg = new GameMessage(player,
            new VerbEditedEvent(target.Name, endedSession.VerbName, endedSession.BufferedLines.Count));
        await emitter.SendGameMessagesAsync([successMsg], ct);

        // Signal client to exit editor mode
        await notifier.NotifyEditorModeExitedAsync(playerId, ct);
    }

    private static Actors.Objects.Object? FindObject(Player player, Actors.Rooms.Room room, ObjectId objectId)
    {
        var inRoom = room.Contents.FirstOrDefault(o => o.Id == objectId);

        if (inRoom is not null)
        {
            return inRoom;
        }

        return player.Inventory.FirstOrDefault(o => o.Id == objectId);
    }
}
