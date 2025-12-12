using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MooSharp.Features.Autocomplete;
using MooSharp.Game;
using MooSharp.Infrastructure.Sessions;
using MooSharp.Web.Services.Auth;
using MooSharp.Web.Services.Session;

namespace MooSharp.Web.Services.SignalR;

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
        var actorId = GetActorIdOrThrow();

        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            { "ConnectionId", Context.ConnectionId },
            { "ActorId", actorId }
        });

        logger.LogInformation("Connection established");

        // Add to player-specific group for targeted notifications (e.g., editor mode changes)
        await Groups.AddToGroupAsync(Context.ConnectionId, actorId.ToString());

        var channel = new SignalROutputChannel(Clients.Caller);

        await gateway.OnSessionStartedAsync(actorId, channel);

        await base.OnConnectedAsync();
    }

    public async Task SendCommand(string command)
    {
        var actor = GetActorIdOrThrow();

        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            { "ConnectionId", Context.ConnectionId },
            { "ActorId", actor }
        });

        logger.LogDebug("Received command from client");

        await engine.ProcessInputAsync(actor, command);
    }

    public async Task<AutocompleteOptions> GetAutocompleteOptions()
    {
        var actor = GetActorIdOrThrow();

        return await engine.GetAutocompleteOptions(actor);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var actorId = GetActorIdOrThrow();

        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            { "ConnectionId", Context.ConnectionId },
            { "ActorId", actorId }
        });

        logger.LogInformation("Connection closed");

        if (exception is not null)
        {
            logger.LogError(exception, "Exception was present on connection loss");
        }

        // Remove from player-specific group
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, actorId.ToString());

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