using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MooSharp.Game;
using MooSharp.Infrastructure;

namespace MooSharp.Web.Services;

[Authorize]
public class MooHub(
    ISessionGateway gateway,
    ActorIdentityResolver identityResolver,
    IGameEngine engine,
    ILogger<MooHub> logger) : Hub
{
    public const string HubName = "/moohub";
    public const string ReceiveMessage = "ReceiveMessage";

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
        
        await engine.ProcessInputAsync(actor, command);
    }

    public async Task<AutocompleteOptions> GetAutocompleteOptions()
    {
        var actor = GetActorIdOrThrow();
        
        return await engine.GetAutocompleteOptions(actor);
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