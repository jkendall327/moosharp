using MooSharp.Commands.Presentation;

namespace MooSharp.Infrastructure.Messaging;

public interface IGameMessageEmitter
{
    Task SendGameMessagesAsync(IEnumerable<GameMessage> messages, CancellationToken ct = default);
}