using MooSharp.Actors;
using MooSharp.Messaging;

namespace MooSharp.Infrastructure;

public interface IRawMessageSender
{
    Task SendLoginRequiredMessageAsync(ConnectionId connectionId, CancellationToken ct = default);

    Task SendSystemMessageAsync(ConnectionId connectionId, string message, CancellationToken ct = default);

    Task SendLoginResultAsync(ConnectionId connectionId, bool success, string message, CancellationToken ct = default);
    Task SendGameMessagesAsync(List<GameMessage> messages, CancellationToken ct = default);
}