using Microsoft.AspNetCore.SignalR;
using MooSharp.Actors;
using MooSharp.Infrastructure;
using MooSharp.Messaging;

namespace MooSharp.Web.Game;

public class SignalRPlayerConnectionFactory(IHubContext<MooHub> hubContext) : IPlayerConnectionFactory
{
    public IPlayerConnection Create(ConnectionId connectionId)
        => new SignalRPlayerConnection(connectionId, hubContext);
}