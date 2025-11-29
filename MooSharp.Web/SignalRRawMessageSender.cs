using Microsoft.AspNetCore.SignalR;
using MooSharp.Infrastructure;

namespace MooSharp.Web;

public class SignalRRawMessageSender(IHubContext<MooHub> hubContext) : IRawMessageSender
{
    public async Task SendLoginRequiredMessageAsync(ConnectionId connectionId, CancellationToken ct = default)
    {
        await hubContext
            .Clients
            .Client(connectionId.Value)
            .SendAsync("ReceiveMessage", "Please log in before sending commands.", cancellationToken: ct);

        await hubContext
            .Clients
            .Client(connectionId.Value)
            .SendAsync("LoginResult", false, "You must log in to play.", cancellationToken: ct);
    }

    public Task SendSystemMessageAsync(ConnectionId connectionId, string message, CancellationToken ct = default)
    {
        return hubContext
            .Clients
            .Client(connectionId.Value)
            .SendAsync("ReceiveMessage", message, cancellationToken: ct);
    }

    public Task SendLoginResultAsync(ConnectionId connectionId, bool success, string message, CancellationToken ct = default)
    {
        return hubContext
            .Clients
            .Client(connectionId.Value)
            .SendAsync("LoginResult", success, message, cancellationToken: ct);
    }
}