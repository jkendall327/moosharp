using Microsoft.Extensions.Logging;
using MooSharp.Commands.Presentation;
using MooSharp.Infrastructure.Sessions;

namespace MooSharp.Infrastructure.Messaging;

public class SessionGatewayMessageEmitter(
    ISessionGateway gateway,
    IGameMessagePresenter presenter,
    ILogger<SessionGatewayMessageEmitter> logger) : IGameMessageEmitter
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