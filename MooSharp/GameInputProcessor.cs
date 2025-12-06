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
            default: throw new ArgumentOutOfRangeException(nameof(input.Command));
        }
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