using System.ComponentModel.DataAnnotations;

namespace MooSharp.Infrastructure;

public class WorldClockOptions
{
    public const string SectionName = "WorldClock";

    [Range(1, int.MaxValue)]
    public int TickIntervalSeconds { get; init; } = 60;

    [Range(1, int.MaxValue)]
    public int DayPeriodDurationMinutes { get; init; } = 10;

    public Dictionary<DayPeriod, string> DayPeriodMessages { get; init; } = new()
    {
        [DayPeriod.Dawn] = "The first light of dawn creeps over the horizon.",
        [DayPeriod.Morning] = "The sun rises, casting long shadows across the land.",
        [DayPeriod.Afternoon] = "The sun reaches its peak, bathing everything in warm light.",
        [DayPeriod.Dusk] = "The sun begins its descent, painting the sky in shades of orange and purple.",
        [DayPeriod.Evening] = "Twilight settles in as the last rays of sunlight fade away.",
        [DayPeriod.Night] = "Darkness blankets the world as stars emerge in the night sky."
    };
}
