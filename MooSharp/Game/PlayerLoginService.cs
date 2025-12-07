using System.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MooSharp.Commands.Presentation;
using MooSharp.Actors.Players;
using MooSharp.Infrastructure.Messaging;
using MooSharp.World;

namespace MooSharp.Game;

/// <summary>
/// Provides messages to players when they log in.
/// Exists because otherwise I ended up with a circular dependency between services.
/// </summary>
public class PlayerLoginService(
    IGameEngine engine,
    IGameMessageEmitter emitter,
    PlayerMessageProvider messageProvider,
    World.World world,
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
        _ = BroadcastPlayerLeftAsync(player);
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

    private async Task BroadcastPlayerLeftAsync(Player player)
    {
        try
        {
            var room = world.GetPlayerLocation(player);

            if (room is null)
            {
                logger.LogWarning("Player {Username} has no known location when leaving the game", player.Username);

                return;
            }

            var leaveEvent = new PlayerLeftGameEvent(player);

            var messages = room.PlayersInRoom
                .Where(p => p != player)
                .Select(p => new GameMessage(p, leaveEvent, MessageAudience.Observer))
                .ToList();

            if (messages.Count is 0)
            {
                return;
            }

            await emitter.SendGameMessagesAsync(messages);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to broadcast player leave message for {Username}", player.Username);
        }
    }
}

public record PlayerLeftGameEvent(Player Player) : IGameEvent;

public class PlayerLeftGameEventFormatter : IGameEventFormatter<PlayerLeftGameEvent>
{
    public string FormatForActor(PlayerLeftGameEvent gameEvent) =>
        $"{gameEvent.Player.Username} has left the game.";

    public string FormatForObserver(PlayerLeftGameEvent gameEvent) =>
        $"{gameEvent.Player.Username} has left the game.";
}