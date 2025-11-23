namespace MooSharp.Messaging;

public class AgentPlayerConnection : IPlayerConnection
{
    public string Id { get; } = Guid.NewGuid().ToString();
    
    public Func<string, Task>? OnMessageReceived { get; set; }

    public async Task SendMessageAsync(string message)
    {
        if (OnMessageReceived is not null)
        {
            await OnMessageReceived(message);
        }
    }
}