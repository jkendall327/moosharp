using System.ComponentModel.DataAnnotations;

namespace MooSharp.Features.Treasure;

public class TreasureSpawnerOptions
{
    public const string SectionName = "TreasureSpawner";

    [Range(1, int.MaxValue)]
    public int SpawnIntervalMinutes { get; init; } = 5;
}
