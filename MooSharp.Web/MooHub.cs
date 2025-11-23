using System.Text;

namespace MooSharp;

using Microsoft.AspNetCore.SignalR;

public class MooHub(
    PlayerMultiplexer connectionManager,
    PlayerGameLoopManager manager,
    World world,
    ILoggerFactory factory,
    ILogger<MooHub> logger,
    IHubContext<MooHub> hubContext) : Hub
{
    public async Task SendCommand(string command)
    {
        logger.LogInformation("Got command {Command}", command);

        var connection = connectionManager.TryGetPlayer(Context.ConnectionId);

        if (connection != null)
        {
            await connection.OnInputReceivedAsync(command);
        }
    }

    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("Connection made with ID {Id}", Context.ConnectionId);

        var defaultRoom = world.Rooms.First().Value;
        
        var player = new Player
        {
            Username = Random
                .Shared
                .Next()
                .ToString(),
            
            CurrentLocation = defaultRoom
        };

        var playerActor = new PlayerActor(player, factory);

        await defaultRoom.AddPlayer(playerActor);

        var connection = new SignalRPlayerConnection(playerActor, hubContext, Context.ConnectionId);

        connection.InputReceived += manager.OnPlayerInput;

        connectionManager.AddPlayer(connection);

        var sb = new StringBuilder();
        sb.AppendLine($"Welcome to the MOO, {player.Username}!");
        await manager.BuildCurrentRoomDescription(playerActor, sb);

        await connection.SendMessageAsync(sb);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation("Connection lost for {Id}", Context.ConnectionId);

        if (exception is not null)
        {
            logger.LogError(exception, "Exception was present on connection loss");
        }

        var connection = connectionManager.TryGetPlayer(Context.ConnectionId);

        if (connection is not null)
        {
            // Remove player from the world.
            // Otherwise you could refresh your ghosts would be left around forever.
            var currentRoom = await connection.Player.GetCurrentRoomAsync();
            await currentRoom.RemovePlayer(connection.Player);

            // Kill the connection.
            await connection.OnConnectionLostAsync();
            connectionManager.RemovePlayer(connection);
        }
        
        await base.OnDisconnectedAsync(exception);
    }
}