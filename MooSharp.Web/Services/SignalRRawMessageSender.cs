using Microsoft.AspNetCore.SignalR;
using MooSharp.Actors;
using MooSharp.Infrastructure;
using MooSharp.Messaging;
using MooSharp.Web.Services;

namespace MooSharp.Web.Game;

public class SignalRRawMessageSender(
    IHubContext<MooHub> hubContext,
    IGameMessagePresenter presenter,
    ILogger<SignalRRawMessageSender> logger) : IRawMessageSender
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

    public Task SendLoginResultAsync(ConnectionId connectionId,
        bool success,
        string message,
        CancellationToken ct = default)
    {
        return hubContext
            .Clients
            .Client(connectionId.Value)
            .SendAsync("LoginResult", success, message, cancellationToken: ct);
    }

    public async Task SendGameMessagesAsync(IEnumerable<GameMessage> messages, CancellationToken ct = default)
    {
        throw new NotImplementedException();
        
        // var tasks = messages
        //     .Select(msg => (msg.Player, Content: presenter.Present(msg)))
        //     .Where(msg => !string.IsNullOrWhiteSpace(msg.Content))
        //     .Select(msg => msg.Player.Connection.SendMessageAsync(msg.Content!, ct));
        //
        // try
        // {
        //     await Task.WhenAll(tasks);
        // }
        // catch (Exception ex)
        // {
        //     logger.LogError(ex, "Error sending messages");
        // }
    }
}