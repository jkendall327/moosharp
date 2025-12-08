using System.Collections.Concurrent;
using MooSharp.Game;
using MooSharp.Infrastructure.Sessions;

namespace MooSharp.Web.Services.Session;

public record Linkdead(Guid ActorId, Timer Timer);

public class SignalRSessionGateway(IGameEngine engine, ILogger<SignalRSessionGateway> logger) : ISessionGateway
{
    private const int MaxQueuedMessages = 50;

    private readonly ConcurrentDictionary<Guid, IOutputChannel> _channels = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentQueue<string>> _linkdeadMessages = new();
    private readonly List<Linkdead> _linkDeads = [];

    public async Task OnSessionStartedAsync(Guid actorId, IOutputChannel channel)
    {
        var dead = _linkDeads.Find(s => s.ActorId == actorId);

        if (dead is not null)
        {
            _linkDeads.Remove(dead);

            await dead.Timer.DisposeAsync();
        }

        _channels.AddOrUpdate(actorId, channel, (_, _) => channel);

        var playerInWorld = engine.IsActorSpawned(actorId);

        if (playerInWorld)
        {
            await ReplayQueuedMessagesAsync(actorId, channel);
        }
        else
        {
            await engine.SpawnActorAsync(actorId);
        }
    }

    public Task OnSessionEndedAsync(Guid actorId)
    {
        _linkdeadMessages.TryAdd(actorId, new());

        var linkdead = new Linkdead(actorId, null!);

        var timer = new Timer(OnLinkdeadTimer, linkdead, TimeSpan.Zero, TimeSpan.FromMinutes(1));

        linkdead = new(actorId, timer);

        _linkDeads.Add(linkdead);

        return Task.CompletedTask;
    }

    // TODO: see if can avoid async avoid here.
    private async void OnLinkdeadTimer(object? state)
    {
        try
        {
            var linkdead = state as Linkdead ?? throw new InvalidOperationException("Timer state was of wrong type.");

            await linkdead.Timer.DisposeAsync();

            _linkDeads.Remove(linkdead);
            _linkdeadMessages.TryRemove(linkdead.ActorId, out _);

            await engine.DespawnActorAsync(linkdead.ActorId);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error when despawning actor from dead session");
        }
    }

    public async Task ForceDisconnectAsync(Guid actorId)
    {
        _channels.TryRemove(actorId, out var _);

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