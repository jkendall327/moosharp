using System.Threading.Channels;

namespace MooSharp;

using Microsoft.AspNetCore.SignalR;

public class MooHub(ChannelWriter<GameInput> writer, ILogger<MooHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("Connection made with ID {Id}", Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    public Task Login(string username, string password)
    {
        writer.TryWrite(new(Context.ConnectionId, new LoginCommand
        {
            Username = username,
            Password = password
        }));

        return Task.CompletedTask;
    }

    public Task Register(string username, string password)
    {
        writer.TryWrite(new(Context.ConnectionId, new RegisterCommand
        {
            Username = username,
            Password = password
        }));

        return Task.CompletedTask;
    }

    public Task SendCommand(string command)
    {
        logger.LogInformation("Got command {Command}", command);
        
        writer.TryWrite(new(Context.ConnectionId, new WorldCommand
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

        writer.TryWrite(new(Context.ConnectionId, new DisconnectCommand()));

        await base.OnDisconnectedAsync(exception);
    }
}