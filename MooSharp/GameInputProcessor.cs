using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooSharp.Actors;
using MooSharp.Agents;
using MooSharp.Commands;
using MooSharp.Commands.Commands;
using MooSharp.Commands.Machinery;
using MooSharp.Data;
using MooSharp.Data.Dtos;
using MooSharp.Data.Mapping;
using MooSharp.Infrastructure;
using MooSharp.Messaging;
using Object = MooSharp.Actors.Object;

namespace MooSharp;

public class GameInputProcessor(
    World.World world,
    CommandParser parser,
    CommandExecutor executor,
    IPlayerRepository playerRepository,
    IRawMessageSender sender,
    IPlayerConnectionFactory connectionFactory,
    ILoginChecker loginChecker,
    PlayerSessionManager sessionManager,
    PlayerHydrator hydrator,
    PlayerMessageProvider messageProvider,
    ILogger<GameInputProcessor> logger)
{
    public async Task ProcessInputAsync(GameInput input, CancellationToken ct = default)
    {
        switch (input.Command)
        {
            case RegisterCommand rc: await CreateNewPlayer(input.ConnectionId, rc, input.SessionToken); break;
            case LoginCommand lc: await Login(input.ConnectionId, lc, input.SessionToken); break;
            case WorldCommand wc:
                if (!world.Players.TryGetValue(input.ConnectionId.Value, out var player))
                {
                    await sender.SendLoginRequiredMessageAsync(input.ConnectionId, ct);

                    break;
                }

                await ProcessWorldCommand(wc, player, ct);

                break;
            case DisconnectCommand:
                await HandleDisconnectAsync(input.ConnectionId, input.SessionToken);

                break;
            case ReconnectCommand:
                await HandleReconnectAsync(input.ConnectionId, input.SessionToken);

                break;
            default: throw new ArgumentOutOfRangeException(nameof(input.Command));
        }
    }

    private async Task HandleDisconnectAsync(ConnectionId connectionId, string? sessionToken)
    {
        if (sessionToken is null)
        {
            return;
        }

        // Ask manager: Is this a valid disconnect for this session?
        var player = sessionManager.StartDisconnect(sessionToken, connectionId);

        if (player is null)
        {
            // Stale disconnect or session already gone
            return;
        }

        // Proceed with game logic (saving state, removing from world map)
        var location = world.GetPlayerLocation(player);

        if (location is null)
        {
            location = world.GetDefaultRoom();

            world.MovePlayer(player, location);
        }

        var snapshot = PlayerSnapshotFactory.CreateSnapshot(player, location);

        await playerRepository.SavePlayerAsync(snapshot);

        world.RemovePlayer(player);

        logger.LogInformation("Player {Player} disconnected (Session kept alive briefly)", player.Username);
    }

    private async Task HandleReconnectAsync(ConnectionId newConnectionId, string? sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            await sender.SendSystemMessageAsync(newConnectionId, "Session missing. Please log in.");

            return;
        }

        // Ask manager: Attempt to resurrect this session
        var player = sessionManager.Reconnect(sessionToken, newConnectionId);

        if (player is null)
        {
            await sender.SendSystemMessageAsync(newConnectionId, "Session expired. Please log in again.");

            return;
        }

        // Session restored! Update Game World state.
        // 1. Clean up the OLD connection ID from the world map if it's still lingering
        var oldConnection = world.Players.FirstOrDefault(x => x.Value == player)
            .Key;

        if (oldConnection != null)
        {
            world.Players.TryRemove(oldConnection, out var _);
        }

        // 2. Refresh connection object on player
        player.Connection = connectionFactory.Create(newConnectionId);

        // 3. Add back to World map
        world.Players[newConnectionId.Value] = player;

        // Tell the client we have logged in.
        await sender.SendLoginResultAsync(newConnectionId, true, "Session restored.");

        logger.LogInformation("Player {Player} reconnected successfully", player.Username);
    }

    private async Task ProcessWorldCommand(WorldCommand command, Player player, CancellationToken ct = default)
    {
        var parsed = await parser.ParseAsync(player, command.Command, ct);

        if (parsed is null)
        {
            _ = sender.SendGameMessagesAsync([
                    new(player, new SystemMessageEvent("That command wasn't recognised."))
                ],
                ct);

            return;
        }

        try
        {
            var result = await executor.Handle(parsed, ct);

            _ = sender.SendGameMessagesAsync(result.Messages, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing world command {Command}", command.Command);

            _ = sender.SendGameMessagesAsync([
                    new(player, new SystemMessageEvent("An unexpected error occurred."))
                ],
                ct);
        }
    }

    private async Task CreateNewPlayer(ConnectionId connectionId, RegisterCommand rc, string? sessionToken)
    {
        var defaultRoom = world.GetDefaultRoom();

        var player = new Player
        {
            Username = rc.Username,
            Connection = connectionFactory.Create(connectionId)
        };

        world.MovePlayer(player, defaultRoom);

        var newPlayerRequest = PlayerSnapshotFactory.CreateNewPlayer(rc.Username, defaultRoom, rc.Password);

        await playerRepository.SaveNewPlayerAsync(newPlayerRequest);

        await DealWithPlayerConnectionBookkeeping(connectionId, sessionToken, player);
    }

    private async Task Login(ConnectionId connectionId, LoginCommand lc, string? sessionToken)
    {
        var result = await loginChecker.LoginIsValidAsync(lc.Username, lc.Password);

        if (result is LoginResult.UsernameNotFound)
        {
            await sender.SendLoginResultAsync(connectionId, false, "No user by that name was found.");

            return;
        }

        if (result is LoginResult.WrongPassword)
        {
            await sender.SendLoginResultAsync(connectionId, false, "Password was incorrect.");

            return;
        }

        if (result is not LoginResult.Ok)
        {
            throw new InvalidOperationException("Unrecognised login failure occurred.");
        }

        var dto = await playerRepository.LoadPlayerAsync(lc.Username);

        if (dto is null)
        {
            throw new InvalidOperationException("No player found even though login check succeeded.");
        }
        
        var player = new Player
        {
            Username = dto.Username,
            Connection = connectionFactory.Create(connectionId)
        };

        await hydrator.RehydrateAsync(player, dto);

        await DealWithPlayerConnectionBookkeeping(connectionId, sessionToken, player);
    }
    
    private async Task DealWithPlayerConnectionBookkeeping(ConnectionId connectionId, string? sessionToken, Player player)
    {
        world.Players[connectionId.Value] = player;
        
        if (!string.IsNullOrWhiteSpace(sessionToken))
        {
            sessionManager.RegisterSession(sessionToken, player, connectionId);
        }

        var messages = await messageProvider.GetMessagesForLogin(player);

        await sender.SendLoginResultAsync(connectionId, true, $"Registered and logged in as {player.Username}.");
        _ = sender.SendGameMessagesAsync(messages);
    }
}