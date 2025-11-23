using Microsoft.Extensions.Options;
using MooSharp.Agents;

namespace MooSharp.Web;

public class AgentBackgroundService(AgentService service, IOptions<AppOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.EnableAgents)
        {
            return;
        }
        
        await service.ExecuteAsync(stoppingToken);
    }
}