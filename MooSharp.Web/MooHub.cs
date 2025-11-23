using System.Threading.Channels;

namespace MooSharp;

using Microsoft.AspNetCore.SignalR;

public class MooHub(ChannelWriter<GameInput> writer, World world, ILogger<MooHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("Connection made with ID {Id}", Context.ConnectionId);

        writer.TryWrite(new(Context.ConnectionId, new WorldCommand()
        {
            Command = "LOGIN"
        }));

        await base.OnConnectedAsync();
    }

    public Task SendCommand(string command)
    {
        logger.LogInformation("Got command {Command}", command);
        
        writer.TryWrite(new(Context.ConnectionId, new WorldCommand()
        {
            Command = command
        }));

        return Task.CompletedTask;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation("Connection lost for {Id}", Context.ConnectionId);

        if (exception is not null)
        {
            logger.LogError(exception, "Exception was present on connection loss");
        }

        var player = world.Players.SingleOrDefault(s => s.ConnectionId == Context.ConnectionId);

        if (player is not null)
        {
            // Remove player from the world.
            // Otherwise you could refresh your ghosts would be left around forever.
            player.CurrentLocation.PlayersInRoom.Remove(player);
        }

        await base.OnDisconnectedAsync(exception);
    }
}