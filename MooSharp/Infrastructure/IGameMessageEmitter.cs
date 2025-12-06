using MooSharp.Actors;
using MooSharp.Messaging;

namespace MooSharp.Infrastructure;

public interface IGameMessageEmitter
{
    Task SendGameMessagesAsync(IEnumerable<GameMessage> messages, CancellationToken ct = default);
}