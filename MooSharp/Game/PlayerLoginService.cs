using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooSharp.Actors.Players;
using MooSharp.Infrastructure.Messaging;

namespace MooSharp.Game;

/// <summary>
/// Provides messages to players when they log in.
/// Exists because otherwise I ended up with a circular dependency between services.
/// </summary>
public class PlayerLoginService(
    IGameEngine engine,
    IGameMessageEmitter emitter,
    PlayerMessageProvider messageProvider,
    ILogger<PlayerLoginService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        engine.OnPlayerSpawned += HandlePlayerSpawned;
        engine.OnPlayerDespawned += HandlePlayerDespawned;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        engine.OnPlayerSpawned -= HandlePlayerSpawned;
        engine.OnPlayerDespawned -= HandlePlayerDespawned;
        return Task.CompletedTask;
    }

    private void HandlePlayerSpawned(Player player)
    {
        _ = SendWelcomeMessagesAsync(player);
    }

    private void HandlePlayerDespawned(Player player)
    {
        logger.LogInformation("Player {Username} has despawned", player.Username);
        // TODO: broadcast a "Has left the game" message to the room here if desired.
    }

    private async Task SendWelcomeMessagesAsync(Player player)
    {
        try
        {
            var messages = await messageProvider.GetMessagesForLogin(player);
            await emitter.SendGameMessagesAsync(messages);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send welcome messages to {Username}", player.Username);
        }
    }
}