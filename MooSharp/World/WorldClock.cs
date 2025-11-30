using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooSharp.Infrastructure;
using MooSharp.Messaging;

namespace MooSharp;

public class WorldClock(
    World world,
    IGameMessagePresenter presenter,
    IOptions<WorldClockOptions> options,
    ILogger<WorldClock> logger) : IWorldClock
{
    public async Task TriggerTickAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (world.Players.IsEmpty)
        {
            return;
        }

        var eventText = GetEventText();

        if (string.IsNullOrWhiteSpace(eventText))
        {
            logger.LogWarning("World clock produced an empty event; skipping broadcast");
            return;
        }

        var gameEvent = new SystemMessageEvent(eventText);

        var sends = world.Players
            .Values
            .Select(player => new GameMessage(player, gameEvent))
            .Select(message => (message.Player, Content: presenter.Present(message)))
            .Where(result => !string.IsNullOrWhiteSpace(result.Content))
            .Select(result => result.Player.Connection.SendMessageAsync(result.Content!, cancellationToken));

        try
        {
            await Task.WhenAll(sends);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error broadcasting world clock event");
        }
    }

    private string GetEventText()
    {
        var events = options.Value.Events;

        if (events.Count == 0)
        {
            return string.Empty;
        }

        var index = Random.Shared.Next(events.Count);

        return events[index];
    }
}
