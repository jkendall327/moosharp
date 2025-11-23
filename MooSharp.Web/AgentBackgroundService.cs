using Microsoft.Extensions.Options;
using MooSharp.Agents;

namespace MooSharp.Web;

public class AgentBackgroundService(AgentSpawner spawner, IOptions<AppOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.EnableAgents)
        {
            return;
        }
        
        await spawner.ExecuteAsync(stoppingToken);
    }
}