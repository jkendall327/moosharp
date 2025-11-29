using Microsoft.AspNetCore.SignalR;
using MooSharp.Messaging;

namespace MooSharp.Web;

public interface IPlayerConnectionFactory
{
    IPlayerConnection Create(ConnectionId connectionId);
}

public class SignalRPlayerConnectionFactory(IHubContext<MooHub> hubContext) : IPlayerConnectionFactory
{
    public IPlayerConnection Create(ConnectionId connectionId) 
        => new SignalRPlayerConnection(connectionId, hubContext);
}