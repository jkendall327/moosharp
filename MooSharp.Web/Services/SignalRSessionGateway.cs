using System.Collections.Concurrent;
using MooSharp.Infrastructure;
using MooSharp.Messaging;

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

    public async Task DispatchToActorAsync(Guid actorId, IGameEvent gameEvent)
    {
        throw new NotImplementedException();
    }

    public async Task BroadcastAsync(IGameEvent gameEvent)
    {
        throw new NotImplementedException();
    }
}