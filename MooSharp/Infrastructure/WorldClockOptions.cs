using System.ComponentModel.DataAnnotations;

namespace MooSharp.Infrastructure;

public class WorldClockOptions
{
    public const string SectionName = "WorldClock";

    [Range(1, int.MaxValue)]
    public int TickIntervalSeconds { get; init; } = 60;

    [MinLength(1)]
    public List<string> Events { get; init; } =
    [
        "The sun begins to set.",
        "It starts to rain."
    ];
}
