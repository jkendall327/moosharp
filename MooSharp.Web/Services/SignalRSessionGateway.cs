using System.Collections.Concurrent;
using MooSharp.Infrastructure;

namespace MooSharp.Web.Services;

public class SignalRSessionGateway : ISessionGateway
{
    private readonly ConcurrentDictionary<Guid, IOutputChannel> _channels = new();

    public Task OnSessionStartedAsync(Guid actorId, IOutputChannel channel)
    {
        _channels.AddOrUpdate(actorId, channel, (_, _) => channel);

        throw new NotImplementedException();
        
        return Task.CompletedTask;
    }

    public async Task OnSessionEndedAsync(Guid actorId)
    {
        throw new NotImplementedException();
    }

    public async Task ForceDisconnectAsync(Guid actorId)
    {
        throw new NotImplementedException();
    }

    public async Task DispatchToActorAsync(Guid actorId, string message, CancellationToken ct = default)
    {
        if (!_channels.TryGetValue(actorId, out var channel))
        {
            return;
        }
        
        await channel.WriteOutputAsync(message, ct);
    }

    public async Task BroadcastAsync(string message, CancellationToken ct = default)
    {
        var all = _channels.Values.ToArray();
        
        var tasks = all.Select(s => s.WriteOutputAsync(message, ct));
        
        await Task.WhenAll(tasks);
    }
}