using System.Text;

namespace MooSharp;

public interface IPlayerConnection
{
    string Id { get; }
    PlayerActor Player { get; }

    Task SendMessageAsync(string message, CancellationToken cancellationToken = default);
    Task SendMessageAsync(StringBuilder message, CancellationToken cancellationToken = default);

    event Func<InputReceivedEvent, Task>? InputReceived;
    
    event Func<Task>? ConnectionLost;

    Task OnInputReceivedAsync(string input);
    
    Task OnConnectionLostAsync();
}

public record InputReceivedEvent(PlayerActor Player, string Input, CancellationToken Token);