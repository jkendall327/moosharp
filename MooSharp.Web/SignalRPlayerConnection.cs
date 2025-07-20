using System.Text;
using Microsoft.AspNetCore.SignalR;

namespace MooSharp;

public class SignalRPlayerConnection : IPlayerConnection
{
    private readonly IHubContext<MooHub> _hubContext;
    public string Id { get; }

    public PlayerActor Player { get; }

    public event Func<InputReceivedEvent, Task>? InputReceived;
    public event Func<Task>? ConnectionLost;

    public SignalRPlayerConnection(PlayerActor player, IHubContext<MooHub> hubContext, string connectionId)
    {
        Player = player;
        Id = connectionId;
        _hubContext = hubContext;
    }

    // Send a message to the specific client this connection represents
    public Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        // "ReceiveMessage" is the name of the method the Blazor client will be listening for.
        return _hubContext.Clients.Client(Id).SendAsync("ReceiveMessage", message, cancellationToken);
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