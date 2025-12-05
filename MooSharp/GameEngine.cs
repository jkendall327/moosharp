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

public class GameEngine(
    World.World world,
    CommandParser parser,
    CommandExecutor executor,
    IPlayerStore playerStore,
    IRawMessageSender rawMessageSender,
    IPlayerConnectionFactory connectionFactory,
    IGameMessagePresenter presenter,
    PlayerSessionManager sessionManager,
    ILogger<GameEngine> logger,
    IOptionsMonitor<AppOptions> appOptions)
{
    private string Motd => (appOptions.CurrentValue.Motd ?? string.Empty).Trim();

    public async Task ProcessInputAsync(GameInput input, CancellationToken ct = default)
    {
        switch (input.Command)
        {
            case RegisterCommand rc: await CreateNewPlayer(input.ConnectionId, rc, input.SessionToken); break;
            case LoginCommand lc: await Login(input.ConnectionId, lc, input.SessionToken); break;
            case RegisterAgentCommand ra: await RegisterAgent(input.ConnectionId, ra); break;
            case AgentThinkingCommand: await HandleAgentThinkingAsync(input.ConnectionId, ct); break;
            case WorldCommand wc:
                if (!world.Players.TryGetValue(input.ConnectionId.Value, out var player))
                {
                    await rawMessageSender.SendLoginRequiredMessageAsync(input.ConnectionId, ct);

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

        await playerStore.SavePlayerAsync(snapshot);

        world.RemovePlayer(player);

        logger.LogInformation("Player {Player} disconnected (Session kept alive briefly)", player.Username);
    }

    private async Task HandleReconnectAsync(ConnectionId newConnectionId, string? sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            await rawMessageSender.SendSystemMessageAsync(newConnectionId, "Session missing. Please log in.");

            return;
        }

        // Ask manager: Attempt to resurrect this session
        var player = sessionManager.Reconnect(sessionToken, newConnectionId);

        if (player is null)
        {
            await rawMessageSender.SendSystemMessageAsync(newConnectionId, "Session expired. Please log in again.");

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
        await rawMessageSender.SendLoginResultAsync(newConnectionId, true, "Session restored.");
        
        logger.LogInformation("Player {Player} reconnected successfully", player.Username);
    }

    private async Task ProcessWorldCommand(WorldCommand command, Player player, CancellationToken ct = default)
    {
        var parsed = await parser.ParseAsync(player, command.Command, ct);

        if (parsed is null)
        {
            _ = rawMessageSender.SendGameMessagesAsync([new(player, new SystemMessageEvent("That command wasn't recognised."))], ct);

            return;
        }

        try
        {
            var result = await executor.Handle(parsed, ct);

            _ = rawMessageSender.SendGameMessagesAsync(result.Messages, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing world command {Command}", command.Command);
            _ = rawMessageSender.SendGameMessagesAsync([new(player, new SystemMessageEvent("An unexpected error occurred."))], ct);
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

        var newPlayerRequest = PlayerSnapshotFactory.CreateNewPlayer(player, defaultRoom, rc.Password);

        await playerStore.SaveNewPlayerAsync(newPlayerRequest);

        world.Players[connectionId.Value] = player;
        TrackSession(sessionToken, player, connectionId);

        var description = BuildCurrentRoomDescription(player);

        var messages = new List<GameMessage>
        {
            new(player, new SystemMessageEvent($"Welcome, {player.Username}."))
        };

        AddMotdMessage(messages, player);

        messages.Add(new(player, new RoomDescriptionEvent(description.ToString())));

        await rawMessageSender.SendLoginResultAsync(connectionId,
            true,
            $"Registered and logged in as {player.Username}.");

        _ = rawMessageSender.SendGameMessagesAsync(messages);
    }

    private async Task Login(ConnectionId connectionId, LoginCommand lc, string? sessionToken)
    {
        var dto = await playerStore.LoadPlayerAsync(new LoginRequest(lc.Username, lc.Password));

        if (dto is null)
        {
            await rawMessageSender.SendSystemMessageAsync(connectionId, "Login failed, please try again.");
            await rawMessageSender.SendLoginResultAsync(connectionId, false, "Login failed, please try again.");

            return;
        }

        var startingRoom = world.Rooms.TryGetValue(new RoomId(dto.CurrentLocation), out var r) ? r : world.GetDefaultRoom();

            var player = new Player
            {
                Username = dto.Username,
                Connection = connectionFactory.Create(connectionId)
            };

            foreach (var item in dto.Inventory)
            {
                var obj = new Object
                {
                    Id = new(Guid.Parse(item.Id)),
                    Name = item.Name,
                    Description = item.Description,
                    Flags = (ObjectFlags)item.Flags,
                    KeyId = item.KeyId,
                    CreatorUsername = item.CreatorUsername
                };

            if (!string.IsNullOrWhiteSpace(item.TextContent))
            {
                obj.WriteText(item.TextContent);
            }

            obj.MoveTo(player);
        }

        world.MovePlayer(player, startingRoom);

        world.Players[connectionId.Value] = player;
        TrackSession(sessionToken, player, connectionId);

        var description = BuildCurrentRoomDescription(player);

        var messages = new List<GameMessage>
        {
            new(player, new SystemMessageEvent($"Welcome back, {player.Username}."))
        };

        AddMotdMessage(messages, player);

        messages.Add(new(player, new RoomDescriptionEvent(description.ToString())));

        await rawMessageSender.SendLoginResultAsync(connectionId, true, $"Logged in as {player.Username}.");

        _ = rawMessageSender.SendGameMessagesAsync(messages);
    }

    private void TrackSession(string? sessionToken, Player player, ConnectionId connectionId)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return;
        }

        sessionManager.RegisterSession(sessionToken, player, connectionId);
    }

    private StringBuilder BuildCurrentRoomDescription(Player player)
    {
        var sb = new StringBuilder();

        var room = world.GetPlayerLocation(player);

        if (room is null)
        {
            logger.LogWarning("Player {Player} has no known location when building description", player.Username);

            sb.AppendLine("You are nowhere.");

            return sb;
        }

        sb.Append(room.DescribeFor(player));

        return sb;
    }

    private void AddMotdMessage(List<GameMessage> messages, Player player)
    {
        var motd = Motd;

        if (string.IsNullOrWhiteSpace(motd))
        {
            return;
        }

        messages.Add(new(player, new SystemMessageEvent(motd)));
    }

    private Task RegisterAgent(ConnectionId connectionId, RegisterAgentCommand command)
    {
        var startingRoom = world.GetDefaultRoom();

        var slug = command.Identity.StartingRoomSlug;

        if (slug is not null)
        {
            if (world.Rooms.TryGetValue(slug, out var found))
            {
                startingRoom = found;
            }
        }

        var player = new Player
        {
            Username = command.Identity.Name,
            Connection = command.Connection
        };

        world.MovePlayer(player, startingRoom);

        world.Players[connectionId.Value] = player;

        logger.LogInformation("Agent {AgentName} registered", player.Username);

        return Task.CompletedTask;
    }

    private async Task HandleAgentThinkingAsync(ConnectionId connectionId, CancellationToken ct)
    {
        if (!world.Players.TryGetValue(connectionId.Value, out var player))
        {
            logger.LogWarning("Could not find player for agent thinking indicator. ConnectionId={ConnectionId}",
                connectionId.Value);

            return;
        }

        var room = world.GetPlayerLocation(player);

        if (room is null)
        {
            logger.LogWarning("Player {Player} has no known location when sending thinking indicator", player.Username);

            return;
        }

        var gameEvent = new AgentThinkingEvent(player);

        var result = new CommandResult();
        result.Add(player, gameEvent);
        result.BroadcastToAllButPlayer(room, player, gameEvent);

        await rawMessageSender.SendGameMessagesAsync(result.Messages, ct);
    }
}
