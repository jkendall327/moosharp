using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooSharp.Commands;
using MooSharp.Infrastructure;
using MooSharp.Messaging;

namespace MooSharp.World;

public class WorldClock(
    World world,
    IOptions<WorldClockOptions> options,
    TimeProvider timeProvider,
    IGameMessageEmitter emitter,
    ILogger<WorldClock> logger) : IWorldClock
{
    private DateTimeOffset _lastPeriodChange = timeProvider.GetUtcNow();

    public async Task TriggerTickAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var now = timeProvider.GetUtcNow();
        var elapsed = now - _lastPeriodChange;
        var periodDuration = TimeSpan.FromMinutes(options.Value.DayPeriodDurationMinutes);

        if (elapsed < periodDuration)
        {
            return;
        }

        _lastPeriodChange = now;

        var nextPeriod = GetNextDayPeriod(world.CurrentDayPeriod);
        world.CurrentDayPeriod = nextPeriod;

        logger.LogInformation("Day period changed to {DayPeriod}", nextPeriod);

        if (world.Players.IsEmpty)
        {
            return;
        }

        var message = GetDayPeriodMessage(nextPeriod);

        if (string.IsNullOrWhiteSpace(message))
        {
            logger.LogWarning("No message configured for day period {DayPeriod}; skipping broadcast", nextPeriod);

            return;
        }

        await BroadcastMessageAsync(message, cancellationToken);
    }

    private static DayPeriod GetNextDayPeriod(DayPeriod current)
    {
        var values = Enum.GetValues<DayPeriod>();
        var currentIndex = Array.IndexOf(values, current);
        var nextIndex = (currentIndex + 1) % values.Length;

        return values[nextIndex];
    }

    private string GetDayPeriodMessage(DayPeriod period)
    {
        return options.Value.DayPeriodMessages.GetValueOrDefault(period, string.Empty);
    }

    private async Task BroadcastMessageAsync(string messageText, CancellationToken cancellationToken)
    {
        var gameEvent = new SystemMessageEvent(messageText);

        var sends = world.Players.Values.Select(player => new GameMessage(player, gameEvent));

        await emitter.SendGameMessagesAsync(sends, cancellationToken);
    }
}