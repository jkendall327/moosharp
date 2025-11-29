using Microsoft.AspNetCore.SignalR;

namespace MooSharp.Messaging;

public class SignalRPlayerConnection(ConnectionId connectionId, IHubContext<MooHub> hubContext) : IPlayerConnection
{
    public string Id => connectionId.Value;

    public async Task SendMessageAsync(string message, CancellationToken ct = default)
    {
        await hubContext.Clients.Client(connectionId.Value).SendAsync("ReceiveMessage", message, cancellationToken: ct);
    }
}