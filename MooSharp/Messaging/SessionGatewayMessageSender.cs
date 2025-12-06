using Microsoft.Extensions.Logging;
using MooSharp.Actors;
using MooSharp.Infrastructure;

namespace MooSharp.Messaging;

public class SessionGatewayMessageSender(
    ISessionGateway gateway,
    IGameMessagePresenter presenter,
    ILogger<SessionGatewayMessageSender> logger) : IRawMessageSender
{
    public Task SendLoginRequiredMessageAsync(ConnectionId connectionId, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public async Task SendSystemMessageAsync(ConnectionId connectionId, string message, CancellationToken ct = default)
    {
        await gateway.DispatchToActorAsync(Guid.NewGuid(), message, ct);
    }

    public Task SendLoginResultAsync(ConnectionId connectionId,
        bool success,
        string message,
        CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public async Task SendGameMessagesAsync(List<GameMessage> messages, CancellationToken ct = default)
    {
        var tasks = messages
            .Select(msg => (msg.Player, Content: presenter.Present(msg)))
            .Where(msg => !string.IsNullOrWhiteSpace(msg.Content))
            .Select(msg => gateway.DispatchToActorAsync(msg.Player.Id.Value, msg.Content!, ct));

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending messages");
        }
    }
}