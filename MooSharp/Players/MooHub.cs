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
        var connection = connectionManager.TryGetPlayer(Context.ConnectionId);

        if (connection != null)
        {
            // This triggers the InputReceived event that the game engine is listening to
            await connection.OnInputReceivedAsync(command);
        }
    }

    public override async Task OnConnectedAsync()
    {
        var atrium = world.Rooms.GetValueOrDefault("atrium");

        if (atrium is null)
        {
            throw new InvalidOperationException("Couldn't find atrium room to set as default location.");
        }

        var player = new Player
        {
            Username = Random.Shared
                             .Next()
                             .ToString(),
            
            CurrentLocation = atrium
        };

        var playerActor = new PlayerActor(player);

        var connection = new SignalRPlayerConnection(playerActor, hubContext, Context.ConnectionId);

        connection.InputReceived += manager.OnPlayerInput;

        connectionManager.AddPlayer(connection);

        await connection.SendMessageAsync($"Welcome to the MOO, {player.Username}!");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connection = connectionManager.TryGetPlayer(Context.ConnectionId);

        if (connection != null)
        {
            // Notify the game engine that the player is gone
            await connection.OnConnectionLostAsync();
            connectionManager.RemovePlayer(connection);
        }

        await base.OnDisconnectedAsync(exception);
    }
}