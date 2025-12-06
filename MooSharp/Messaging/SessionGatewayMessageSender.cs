using Microsoft.Extensions.Logging;
using MooSharp.Infrastructure;

namespace MooSharp.Messaging;

public class SessionGatewayMessageSender(
    ISessionGateway gateway,
    IGameMessagePresenter presenter,
    ILogger<SessionGatewayMessageSender> logger) : IRawMessageSender
{
    public async Task SendGameMessagesAsync(IEnumerable<GameMessage> messages, CancellationToken ct = default)
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