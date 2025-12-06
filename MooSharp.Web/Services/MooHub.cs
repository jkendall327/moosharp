using System.Threading.Channels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MooSharp.Game;
using MooSharp.Infrastructure;
using MooSharp.Messaging;

namespace MooSharp.Web.Services;

[Authorize]
public class MooHub(
    ISessionGateway gateway,
    ActorIdentityResolver identityResolver,
    ChannelWriter<NewGameInput> writer,
    ILogger<MooHub> logger,
    World.World world) : Hub
{
    public const string HubName = "/moohub";

    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("Connection made with ID {Id}", Context.ConnectionId);

        var actorId = GetActorIdOrThrow();

        var channel = new SignalROutputChannel(Clients.Caller);

        await gateway.OnSessionStartedAsync(actorId, channel);

        await base.OnConnectedAsync();
    }

    public async Task SendCommand(string command)
    {
        logger.LogInformation("Got command {Command}", command);

        var actor = GetActorIdOrThrow();
        
        await writer.WriteAsync(new(actor, command));
    }

    public Task<AutocompleteOptions> GetAutocompleteOptions()
    {
        throw new NotImplementedException();
        
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

        var actorId = GetActorIdOrThrow();

        await gateway.OnSessionEndedAsync(actorId);

        await base.OnDisconnectedAsync(exception);
    }

    private Guid GetActorIdOrThrow()
    {
        var user = Context.User;

        if (user is null)
        {
            throw new InvalidOperationException("Claims principal was null despite authorization.");
        }

        var actorId = identityResolver.GetActorId(user);

        if (actorId is null)
        {
            throw new InvalidOperationException("Actor ID not found");
        }

        return actorId.Value;
    }
}