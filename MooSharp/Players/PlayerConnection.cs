using System.Text;
using Microsoft.AspNetCore.SignalR;

namespace MooSharp;

public interface IPlayerConnection
{
    Guid Id { get; }
    PlayerActor Player { get; }

    Task SendMessageAsync(string message, CancellationToken cancellationToken = default);
    Task SendMessageAsync(StringBuilder message, CancellationToken cancellationToken = default);

    event Func<InputReceivedEvent, Task>? InputReceived;
    
    event Func<Task>? ConnectionLost;

    Task OnInputReceivedAsync(string input);
    
    Task OnConnectionLostAsync();
}

public record InputReceivedEvent(PlayerActor Player, string Input, CancellationToken Token);

public class SignalRPlayerConnection : IPlayerConnection
{
    private readonly IHubContext<MooHub> _hubContext;
    private readonly string _connectionId;

    public Guid Id { get; } = Guid.CreateVersion7();
    public PlayerActor Player { get; }

    public event Func<InputReceivedEvent, Task>? InputReceived;
    public event Func<Task>? ConnectionLost;

    public SignalRPlayerConnection(PlayerActor player, IHubContext<MooHub> hubContext, string connectionId)
    {
        Player = player;
        _hubContext = hubContext;
        _connectionId = connectionId;
    }

    // Send a message to the specific client this connection represents
    public Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        // "ReceiveMessage" is the name of the method the Blazor client will be listening for.
        return _hubContext.Clients.Client(_connectionId).SendAsync("ReceiveMessage", message, cancellationToken);
    }

    public Task SendMessageAsync(StringBuilder message, CancellationToken cancellationToken = default)
    {
        // StringBuilder doesn't have a direct SendAsync overload, so we convert it.
        return SendMessageAsync(message.ToString(), cancellationToken);
    }
    
    public async Task OnInputReceivedAsync(string input)
    {
        if (InputReceived != null)
        {
            await InputReceived.Invoke(new(Player, input, CancellationToken.None));
        }
    }

    // Called by the SignalR Hub when the client disconnects
    public async Task OnConnectionLostAsync()
    {
        if (ConnectionLost != null)
        {
            await ConnectionLost.Invoke();
        }
    }
}