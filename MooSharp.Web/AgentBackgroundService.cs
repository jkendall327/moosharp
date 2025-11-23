using MooSharp.Agents;

namespace MooSharp.Web;

public class AgentBackgroundService(AgentService service) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await service.ExecuteAsync(stoppingToken);
    }
}