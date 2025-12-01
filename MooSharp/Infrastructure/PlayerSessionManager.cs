using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using MooSharp.Actors;

namespace MooSharp.Infrastructure;

public class PlayerSessionManager(TimeProvider clock, ILogger<PlayerSessionManager> logger)
{
    private readonly ConcurrentDictionary<string, SessionState> _sessions = new(StringComparer.Ordinal);
    private static readonly TimeSpan SessionGracePeriod = TimeSpan.FromSeconds(10);

    private class SessionState
    {
        public required Player Player { get; init; }
        public required string ConnectionId { get; set; }
        public CancellationTokenSource? CleanupCts { get; set; }
    }

    public void RegisterSession(string sessionToken, Player player, ConnectionId connectionId)
    {
        // Cancel any pending cleanup if we are overwriting an existing session
        if (_sessions.TryGetValue(sessionToken, out var existing))
        {
            CancelCleanup(existing);
        }

        _sessions[sessionToken] = new()
        {
            Player = player,
            ConnectionId = connectionId.Value
        };
    }

    /// <summary>
    /// Attempts to reconnect a session. Returns the Player object if successful.
    /// </summary>
    public Player? Reconnect(string sessionToken, ConnectionId newConnectionId)
    {
        if (string.IsNullOrWhiteSpace(sessionToken) || !_sessions.TryGetValue(sessionToken, out var session))
        {
            return null;
        }

        // Stop the session from being deleted
        CancelCleanup(session);

        var oldConnection = session.ConnectionId;
        session.ConnectionId = newConnectionId.Value;

        logger.LogInformation("Session {SessionToken} reconnected. Old: {OldId}, New: {NewId}",
            sessionToken,
            oldConnection,
            newConnectionId.Value);

        return session.Player;
    }

    /// <summary>
    /// Returns the Player associated with the connection, but only if the session matches the provided token.
    /// Used to validate disconnects (prevent stale disconnects from killing active sessions).
    /// </summary>
    public Player? StartDisconnect(string sessionToken, ConnectionId connectionId)
    {
        if (string.IsNullOrWhiteSpace(sessionToken) || !_sessions.TryGetValue(sessionToken, out var session))
        {
            return null;
        }

        // If the connection ID doesn't match, this is a disconnect event 
        // from an old connection (race condition or lag), ignore it.
        if (!string.Equals(session.ConnectionId, connectionId.Value, StringComparison.Ordinal))
        {
            logger.LogInformation("Ignoring stale disconnect for {ConnectionId}", connectionId.Value);

            return null;
        }

        // Start the countdown to remove the session from memory
        ScheduleCleanup(sessionToken, session);

        return session.Player;
    }

    public void RemoveSession(string sessionToken)
    {
        if (_sessions.TryRemove(sessionToken, out var session))
        {
            CancelCleanup(session);
        }
    }

    private void ScheduleCleanup(string token, SessionState session)
    {
        CancelCleanup(session);

        session.CleanupCts = new();
        var ct = session.CleanupCts.Token;

        _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(SessionGracePeriod, clock, ct);

                    if (_sessions.TryRemove(token, out var _))
                    {
                        logger.LogInformation("Session {Token} expired and was removed", token);
                    }
                }
                catch (TaskCanceledException)
                {
                    // Reconnected in time
                }
            },
            ct);
    }

    private static void CancelCleanup(SessionState session)
    {
        if (session.CleanupCts is null)
        {
            return;
        }

        try
        {
            session.CleanupCts.Cancel();
            session.CleanupCts.Dispose();
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            session.CleanupCts = null;
        }
    }
}