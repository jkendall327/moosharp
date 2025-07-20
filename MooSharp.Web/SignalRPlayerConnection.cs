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

    public Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients.Client(Id).SendAsync("ReceiveMessage", message, cancellationToken);
    }

    public Task SendMessageAsync(StringBuilder message, CancellationToken cancellationToken = default)
    {
        return SendMessageAsync(message.ToString(), cancellationToken);
    }
    
    public async Task OnInputReceivedAsync(string input)
    {
        if (InputReceived != null)
        {
            await InputReceived.Invoke(new(Player, input, CancellationToken.None));
        }
    }

    public async Task OnConnectionLostAsync()
    {
        if (ConnectionLost != null)
        {
            await ConnectionLost.Invoke();
        }
    }
}