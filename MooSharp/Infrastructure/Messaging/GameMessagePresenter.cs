using Microsoft.Extensions.Logging;
using MooSharp.Commands.Presentation;

namespace MooSharp.Infrastructure.Messaging;

public interface IGameMessagePresenter
{
    string? Present(GameMessage message);
}

public class GameMessagePresenter(IEnumerable<IGameEventFormatter> formatters, ILogger<GameMessagePresenter> logger) : IGameMessagePresenter
{
    public string? Present(GameMessage message)
    {
        var formatter = formatters.FirstOrDefault(f => f.CanFormat(message.Event));

        if (formatter is not null)
        {
            return message.Audience switch
            {
                MessageAudience.Actor => formatter.FormatForActor(message.Event),
                MessageAudience.Observer => formatter.FormatForObserver(message.Event),
                var _ => throw new InvalidOperationException(
                    $"Invalid audience supplied for message: {message.Audience}")
            };
        }

        logger.LogWarning("No formatter registered for event type {EventType}", message.Event.GetType().Name);
        return string.Empty;

    }
}
