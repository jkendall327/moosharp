using System.Threading.Channels;

namespace MooSharp;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Primitives;

public class MooHub(ChannelWriter<GameInput> writer, ILogger<MooHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("Connection made with ID {Id}", Context.ConnectionId);

        writer.TryWrite(new(Context.ConnectionId, new ReconnectCommand(), GetSessionId()));

        await base.OnConnectedAsync();
    }

    public Task Login(string username, string password)
    {
        writer.TryWrite(new(Context.ConnectionId, new LoginCommand
        {
            Username = username,
            Password = password
        }, GetSessionId()));

        return Task.CompletedTask;
    }

    public Task Register(string username, string password)
    {
        writer.TryWrite(new(Context.ConnectionId, new RegisterCommand
        {
            Username = username,
            Password = password
        }, GetSessionId()));

        return Task.CompletedTask;
    }

    public Task SendCommand(string command)
    {
        logger.LogInformation("Got command {Command}", command);

        writer.TryWrite(new(Context.ConnectionId, new WorldCommand
        {
            Command = command
        }, GetSessionId()));

        return Task.CompletedTask;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation("Connection lost for {Id}", Context.ConnectionId);

        if (exception is not null)
        {
            logger.LogError(exception, "Exception was present on connection loss");
        }

        writer.TryWrite(new(Context.ConnectionId, new DisconnectCommand(), GetSessionId()));

        await base.OnDisconnectedAsync(exception);
    }

    private string? GetSessionId()
    {
        var context = Context.GetHttpContext();

        if (context is null)
        {
            return null;
        }

        if (context.Request.Query.TryGetValue("access_token", out var token))
        {
            return GetFirstValue(token);
        }

        if (context.Request.Headers.TryGetValue("x-session-id", out var headerToken))
        {
            return GetFirstValue(headerToken);
        }

        return null;
    }

    private static string? GetFirstValue(StringValues values) =>
        values.Count == 0 ? null : values[0];
}
