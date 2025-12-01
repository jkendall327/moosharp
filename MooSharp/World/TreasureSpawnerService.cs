using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MooSharp.Infrastructure;

namespace MooSharp;

public class TreasureSpawnerService(
    World world,
    IOptions<TreasureSpawnerOptions> options,
    TimeProvider timeProvider) : BackgroundService
{
    private static readonly IReadOnlyList<(string Name, string Description, decimal Value)> TreasureTemplates =
    [
        ("a glittering ruby", "A deep red gemstone that catches the light beautifully.", 15.00m),
        ("an emerald", "A brilliant green gem, cool to the touch.", 12.50m),
        ("a sapphire", "A stunning blue gemstone with flecks of white.", 14.00m),
        ("a gold coin", "A shiny gold coin stamped with an ancient seal.", 5.00m),
        ("a silver trinket", "A small silver ornament of uncertain purpose.", 3.50m),
        ("a pearl", "A lustrous white pearl, perfectly round.", 8.00m),
        ("an amethyst", "A purple crystal that seems to glow faintly.", 10.00m),
        ("a jade figurine", "A small carved figure made of green jade.", 20.00m),
        ("a diamond chip", "A tiny but brilliant fragment of diamond.", 25.00m),
        ("a copper bracelet", "A simple but well-crafted bracelet.", 2.00m)
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(options.Value.SpawnIntervalMinutes);

        using var timer = new PeriodicTimer(interval, timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var treasure = CreateRandomTreasure();
            world.SpawnTreasureInEmptyRoom([treasure]);
        }
    }

    private static Object CreateRandomTreasure()
    {
        var template = TreasureTemplates[Random.Shared.Next(TreasureTemplates.Count)];

        return new()
        {
            Name = template.Name,
            Description = template.Description,
            Value = template.Value
        };
    }
}
