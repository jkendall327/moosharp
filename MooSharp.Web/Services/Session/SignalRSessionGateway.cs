using System.Collections.Concurrent;
using MooSharp.Game;
using MooSharp.Infrastructure;
using MooSharp.Infrastructure.Sessions;

namespace MooSharp.Web.Services.Session;

public class SignalRSessionGateway(
    IGameEngine engine,
    MooSharpMetrics metrics,
    ILogger<SignalRSessionGateway> logger) : ISessionGateway
{
    private const int MaxQueuedMessages = 50;

    private readonly ConcurrentDictionary<Guid, IOutputChannel> _channels = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentQueue<string>> _linkdeadMessages = new();
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _linkdeadCts = new();

    public event Action<Guid>? OnSessionReconnected;

    public async Task OnSessionStartedAsync(Guid actorId, IOutputChannel channel)
    {
        // cancel linkdead if any
        if (_linkdeadCts.TryRemove(actorId, out var cts))
        {
            metrics.RecordPlayerLinkdeadRecovered();
            cts.Cancel();
            cts.Dispose();
        }

        _channels.AddOrUpdate(actorId, channel, (_, _) => channel);

        var playerInWorld = engine.IsActorSpawned(actorId);

        if (playerInWorld)
        {
            await ReplayQueuedMessagesAsync(actorId, channel);
            OnSessionReconnected?.Invoke(actorId);
        }
        else
        {
            await engine.SpawnActorAsync(actorId);
        }
    }

    public Task OnSessionEndedAsync(Guid actorId)
    {
        _linkdeadMessages.TryAdd(actorId, new());

        var cts = new CancellationTokenSource();

        if (!_linkdeadCts.TryAdd(actorId, cts))
        {
            cts.Dispose();

            return Task.CompletedTask;
        }

        metrics.RecordPlayerLinkdead();

        _ = HandleLinkdeadAsync(actorId, cts.Token);

        return Task.CompletedTask;
    }

    private async Task HandleLinkdeadAsync(Guid actorId, CancellationToken ct)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(1), ct);

            if (ct.IsCancellationRequested)
            {
                return;
            }

            _linkdeadMessages.TryRemove(actorId, out _);

            // Linkdead period expired - player is being despawned
            metrics.RecordPlayerLinkdeadRecovered();

            await engine.DespawnActorAsync(actorId);
        }
        catch (TaskCanceledException)
        {
            // swallow - player reconnected
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error when despawning actor from dead session");
        }
    }

    public async Task ForceDisconnectAsync(Guid actorId)
    {
        _channels.TryRemove(actorId, out var _);

        if (_linkdeadCts.TryRemove(actorId, out var cts))
        {
            metrics.RecordPlayerLinkdeadRecovered();
            cts.Cancel();
            cts.Dispose();
        }

        if (engine.IsActorSpawned(actorId))
        {
            await engine.DespawnActorAsync(actorId);
        }
    }

    public async Task DispatchToActorAsync(Guid actorId, string message, CancellationToken ct = default)
    {
        if (!_channels.TryGetValue(actorId, out var channel))
        {
            QueueMessageForLinkdead(actorId, message);

            return;
        }

        await channel.WriteOutputAsync(message, ct);
    }

    private void QueueMessageForLinkdead(Guid actorId, string message)
    {
        if (!_linkdeadMessages.TryGetValue(actorId, out var queue))
        {
            return;
        }

        if (queue.Count >= MaxQueuedMessages)
        {
            queue.TryDequeue(out _);
        }

        queue.Enqueue(message);
    }

    private async Task ReplayQueuedMessagesAsync(Guid actorId, IOutputChannel channel)
    {
        if (!_linkdeadMessages.TryRemove(actorId, out var queue))
        {
            return;
        }

        while (queue.TryDequeue(out var message))
        {
            await channel.WriteOutputAsync(message);
        }
    }

    public async Task BroadcastAsync(string message, CancellationToken ct = default)
    {
        var all = _channels.Values.ToArray();

        var tasks = all.Select(s => s.WriteOutputAsync(message, ct));

        await Task.WhenAll(tasks);
    }
}
