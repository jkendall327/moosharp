using MooSharp.Actors;
using MooSharp.Messaging;

namespace MooSharp.Infrastructure;

public interface IRawMessageSender
{
    Task SendGameMessagesAsync(IEnumerable<GameMessage> messages, CancellationToken ct = default);
}