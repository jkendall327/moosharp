using Microsoft.AspNetCore.SignalR;
using MooSharp.Infrastructure;
using MooSharp.Messaging;

namespace MooSharp.Web;

public class SignalRPlayerConnectionFactory(IHubContext<MooHub> hubContext) : IPlayerConnectionFactory
{
    public IPlayerConnection Create(ConnectionId connectionId)
        => new SignalRPlayerConnection(connectionId, hubContext);
}