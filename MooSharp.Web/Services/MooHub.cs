using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using MooSharp.Game;
using MooSharp.Messaging;

namespace MooSharp.Web.Services;

public class MooHub(ChannelWriter<GameInput> writer, ILogger<MooHub> logger, World.World world) : Hub
{
    public const string HubName = "/moohub";
    
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

    public Task<AutocompleteOptions> GetAutocompleteOptions()
    {
        if (!world.Players.TryGetValue(Context.ConnectionId, out var player))
        {
            return Task.FromResult(new AutocompleteOptions([], []));
        }

        var room = world.GetPlayerLocation(player);

        var exits = room?.Exits.Keys ?? Enumerable.Empty<string>();
        var inventory = player.Inventory.Select(item => item.Name);

        var options = new AutocompleteOptions(exits.ToList(), inventory.ToList());

        return Task.FromResult(options);
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
        
        // .NET SignalR client: AccessTokenProvider -> Authorization: Bearer {token}
        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            return null;
        }

        var value = authHeader.ToString();
        const string bearerPrefix = "Bearer ";

        return value.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase) ? value[bearerPrefix.Length..].Trim() : null;
    }
}
