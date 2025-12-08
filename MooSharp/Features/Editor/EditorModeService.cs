using System.Collections.Concurrent;
using MooSharp.Actors.Objects;

namespace MooSharp.Features.Editor;

public class EditorModeService : IEditorModeService
{
    private readonly ConcurrentDictionary<Guid, EditorSession> _sessions = new();

    public void StartSession(Guid playerId, ObjectId targetObjectId, string targetObjectName, string verbName)
    {
        var session = new EditorSession
        {
            PlayerId = playerId,
            TargetObjectId = targetObjectId,
            TargetObjectName = targetObjectName,
            VerbName = verbName
        };

        _sessions[playerId] = session;
    }

    public bool IsInEditorMode(Guid playerId) => _sessions.ContainsKey(playerId);

    public EditorSession? GetSession(Guid playerId)
    {
        _sessions.TryGetValue(playerId, out var session);
        return session;
    }

    public void AddLine(Guid playerId, string line)
    {
        if (_sessions.TryGetValue(playerId, out var session))
        {
            session.BufferedLines.Add(line);
        }
    }

    public EditorSession? EndSession(Guid playerId)
    {
        _sessions.TryRemove(playerId, out var session);
        return session;
    }

    public void CancelSession(Guid playerId)
    {
        _sessions.TryRemove(playerId, out _);
    }
}
