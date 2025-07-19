using System.Text;
using Microsoft.AspNetCore.SignalR;

namespace MooSharp;

public interface IPlayerConnection
{
    Guid Id { get; }
    Player Player { get; }

    Task SendMessageAsync(string message, CancellationToken cancellationToken = default);
    Task SendMessageAsync(StringBuilder message, CancellationToken cancellationToken = default);

    event Func<string, Task>? InputReceived;
    
    event Func<Task>? ConnectionLost;

    Task OnInputReceivedAsync(string input);
    
    Task OnConnectionLostAsync();
}

public class SignalRPlayerConnection : IPlayerConnection
{
    private readonly IHubContext<MooHub> _hubContext;
    private readonly string _connectionId;

    public Guid Id { get; } = Guid.CreateVersion7();
    public Player Player { get; }

    public event Func<string, Task>? InputReceived;
    public event Func<Task>? ConnectionLost;

    public SignalRPlayerConnection(Player player, IHubContext<MooHub> hubContext, string connectionId)
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
    
    // Called by the SignalR Hub when it receives a command from the client
    public async Task OnInputReceivedAsync(string input)
    {
        if (InputReceived != null)
        {
            await InputReceived.Invoke(input);
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

// public class StreamBasedPlayerConnection : IPlayerConnection
// {
//     private readonly StreamReader _reader;
//     private readonly StreamWriter _writer;
//
//     public Guid Id { get; set; } = Guid.CreateVersion7();
//     public Player Player { get; private set; }
//
//     public StreamBasedPlayerConnection(Stream stream, Player player)
//     {
//         Player = player;
//
//         _reader = new(stream);
//
//         _writer = new(stream)
//         {
//             AutoFlush = true
//         };
//     }
//
//     public async Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
//     {
//         await _writer.WriteLineAsync(message);
//     }
//
//     public async Task SendMessageAsync(StringBuilder message, CancellationToken cancellationToken = default)
//     {
//         await _writer.WriteLineAsync(message, cancellationToken);
//     }
//
//     public async Task<string?> GetStringAsync(CancellationToken cancellationToken = default)
//     {
//         var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
//
//         var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
//
//         return await _reader.ReadLineAsync(linked.Token);
//     }
// }