using MooSharp.Infrastructure.Sessions;

namespace MooSharp.Agents;

public class AgentOutputChannel(Func<string, Task> onMessageReceived) : IOutputChannel
{
    public async Task WriteOutputAsync(string message, CancellationToken ct = default)
    {
        await onMessageReceived(message);
    }
}