using Microsoft.AspNetCore.SignalR;
using MooSharp.Actors;
using MooSharp.Messaging;
using MooSharp.Web.Services;

namespace MooSharp.Web.Game;

public class SignalRPlayerConnection(ConnectionId connectionId, IHubContext<MooHub> hubContext) : IPlayerConnection
{
    public string Id => connectionId.Value;

    public async Task SendMessageAsync(string message, CancellationToken ct = default)
    {
        await hubContext.Clients.Client(connectionId.Value).SendAsync("ReceiveMessage", message, cancellationToken: ct);
    }
}