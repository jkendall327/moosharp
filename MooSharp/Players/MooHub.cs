namespace MooSharp;

using Microsoft.AspNetCore.SignalR;

public class MooHub(
    PlayerMultiplexer connectionManager,
    PlayerGameLoopManager manager,
    IHubContext<MooHub> hubContext,
    IPlayerRepository playerRepository) : Hub
{
    public async Task SendCommand(string command)
    {
        IPlayerConnection connection = connectionManager._connections.First().Value;
        if (connection != null)
        {
            // This triggers the InputReceived event that the game engine is listening to
            await connection.OnInputReceivedAsync(command);
        }
    }

    public override async Task OnConnectedAsync()
    {
        // This is where you link the SignalR connection to a Player
        // The user should already be authenticated via ASP.NET Core Identity.
        var userIdentifier = Context.UserIdentifier!; // From claims principal
        var player = await playerRepository.GetPlayerByIdentifierAsync(userIdentifier);

        // Create our new connection type
        var connection = new SignalRPlayerConnection(player, hubContext, Context.ConnectionId);

        connection.InputReceived += manager.OnPlayerInput;
        
        // --- HOOK UP YOUR GAME ENGINE HERE ---
        // For example, get your command parser and subscribe it to the event
        // var commandParser = GetYourGameEngine().CommandParser;
        // connection.InputReceived += commandParser.ParseAsync; 
        // connection.ConnectionLost += async () => { /* Log player out logic */ };
        // ------------------------------------

        connectionManager.AddPlayer(connection);
        
        await connection.SendMessageAsync("Welcome to the MOO!");
        // You might trigger a "look" command here automatically
        await connection.OnInputReceivedAsync("look"); 

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        IPlayerConnection connection = connectionManager._connections.First().Value;
        
        if (connection != null)
        {
            // Notify the game engine that the player is gone
            await connection.OnConnectionLostAsync();
        }

        connectionManager.RemovePlayer(connection);
        
        await base.OnDisconnectedAsync(exception);
    }
}

// Dummy interface for demonstration
public interface IPlayerRepository { Task<Player> GetPlayerByIdentifierAsync(string id); }