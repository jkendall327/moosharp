namespace MooSharp;

using Microsoft.AspNetCore.SignalR;

public class MooHub(
    PlayerMultiplexer connectionManager,
    PlayerGameLoopManager manager,
    World world,
    IHubContext<MooHub> hubContext) : Hub
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
        
        var player = new Player
        {
            Username = Random.Shared.Next().ToString(),
            CurrentLocation = world.Rooms.First()
        };

        var playerActor = new PlayerActor(player);

        var connection = new SignalRPlayerConnection(playerActor, hubContext, Context.ConnectionId);

        connection.InputReceived += manager.OnPlayerInput;
        
        connectionManager.AddPlayer(connection);
        
        await connection.SendMessageAsync("Welcome to the MOO!");

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