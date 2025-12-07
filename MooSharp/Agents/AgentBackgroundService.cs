using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MooSharp.Agents;

namespace MooSharp.Web.Game;

public class AgentBackgroundService(AgentSpawner spawner, IOptions<AgentOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        await spawner.ExecuteAsync(stoppingToken);
    }
}