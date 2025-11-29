using System.Text;
using System.Threading.Channels;
using MooSharp.Messaging;
using MooSharp.Agents;
using MooSharp.Infrastructure;
using MooSharp.Persistence;

namespace MooSharp;

public class GameEngine(
    World world,
    CommandParser parser,
    CommandExecutor executor,
    ChannelReader<GameInput> reader,
    IPlayerStore playerStore,
    IRawMessageSender rawMessageSender,
    IPlayerConnectionFactory connectionFactory,
    ILogger<GameEngine> logger,
    IGameMessagePresenter presenter) : BackgroundService
{
    private static readonly TimeSpan SessionGracePeriod = TimeSpan.FromSeconds(10);

    private readonly Dictionary<string, Player> _sessionPlayers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _sessionConnections = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CancellationTokenSource> _sessionCleanupTokens = new(StringComparer.Ordinal);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var input in reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessInput(input, stoppingToken);

                // Signal success
                input.CompletionSource?.TrySetResult();
            }
            catch (Exception ex)
            {
                input.CompletionSource?.TrySetException(ex);
                logger.LogError(ex, "Error processing input");
            }
        }
    }

    private async Task ProcessInput(GameInput input, CancellationToken ct = default)
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

                await ProcessWorldCommand(wc, ct, player);

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
        if (sessionToken is not null && _sessionConnections.TryGetValue(sessionToken, out var trackedConnection) &&
            !string.Equals(trackedConnection, connectionId.Value, StringComparison.Ordinal))
        {
            logger.LogInformation("Ignoring disconnect for stale connection {ConnectionId} (session {SessionId})",
                connectionId,
                sessionToken);

            return;
        }

        if (!world.Players.TryGetValue(connectionId.Value, out var player))
        {
            logger.LogWarning("Player with connection {ConnectionId} not found during disconnect", connectionId);

            return;
        }

        var location = world.GetPlayerLocation(player);

        if (location is null)
        {
            logger.LogWarning("Player {Player} has no known location during disconnect", player.Username);

            location = world.Rooms.First()
                .Value;

            world.MovePlayer(player, location);
        }

        await playerStore.SavePlayer(player, location);
        world.RemovePlayer(player);

        world.Players.TryRemove(connectionId.Value, out _);

        if (sessionToken is not null)
        {
            ScheduleSessionCleanup(sessionToken);
        }

        logger.LogInformation("Player {Player} disconnected", player.Username);
    }

    private Task HandleReconnectAsync(ConnectionId newConnectionId, string? sessionToken)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            logger.LogWarning("Reconnect attempted without session token for {ConnectionId}", newConnectionId);

            return rawMessageSender.SendSystemMessageAsync(newConnectionId, "Session missing. Please log in again.");
        }

        if (!_sessionPlayers.TryGetValue(sessionToken, out var player))
        {
            logger.LogWarning("No player found for session {SessionId} during reconnect", sessionToken);

            return rawMessageSender.SendSystemMessageAsync(newConnectionId, "Session expired. Please log in again.");
        }

        CancelScheduledCleanup(sessionToken);

        var oldConnectionId = _sessionConnections.GetValueOrDefault(sessionToken);

        if (!string.IsNullOrEmpty(oldConnectionId))
        {
            world.Players.TryRemove(oldConnectionId, out _);
        }

        player.Connection = connectionFactory.Create(newConnectionId);

        world.Players[newConnectionId.Value] = player;
        _sessionConnections[sessionToken] = newConnectionId.Value;

        logger.LogInformation(
            "Player {Player} reconnected. OldConnection={OldConnection} NewConnection={NewConnection}",
            player.Username,
            oldConnectionId,
            newConnectionId.Value);

        return player.Connection.SendMessageAsync("Reconnected to your active session.");
    }

    private void ScheduleSessionCleanup(string sessionToken)
    {
        CancelScheduledCleanup(sessionToken);

        var cts = new CancellationTokenSource();
        _sessionCleanupTokens[sessionToken] = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(SessionGracePeriod, cts.Token);

                _sessionPlayers.Remove(sessionToken);
                _sessionConnections.Remove(sessionToken);
                _sessionCleanupTokens.Remove(sessionToken);

                logger.LogInformation("Session {SessionId} removed after disconnect grace period of {GracePeriod}",
                    sessionToken,
                    SessionGracePeriod);
            }
            catch (TaskCanceledException)
            {
                logger.LogInformation("Cleanup cancelled for session {SessionId}", sessionToken);
            }
            finally
            {
                cts.Dispose();
            }
        },
        cts.Token);
    }

    private void CancelScheduledCleanup(string sessionToken)
    {
        if (_sessionCleanupTokens.Remove(sessionToken, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }

    private async Task ProcessWorldCommand(WorldCommand command, CancellationToken ct, Player player)
    {
        var parsed = await parser.ParseAsync(player, command.Command, ct);

        if (parsed is null)
        {
            _ = SendGameMessagesAsync([new(player, new SystemMessageEvent("That command wasn't recognised."))], ct);

            return;
        }

        try
        {
            var result = await executor.Handle(parsed, ct);

            _ = SendGameMessagesAsync(result.Messages, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing world command {Command}", command.Command);
            _ = SendGameMessagesAsync([new(player, new SystemMessageEvent("An unexpected error occurred."))], ct);
        }
    }

    private async Task CreateNewPlayer(ConnectionId connectionId, RegisterCommand rc, string? sessionToken)
    {
        var defaultRoom = world.Rooms.First()
            .Value;

        var player = new Player
        {
            Username = rc.Username,
            Connection = connectionFactory.Create(connectionId)
        };

        world.MovePlayer(player, defaultRoom);

        await playerStore.SaveNewPlayer(player, defaultRoom, rc.Password);

        world.Players[connectionId.Value] = player;
        TrackSession(sessionToken, player, connectionId);

        var description = BuildCurrentRoomDescription(player);

        var messages = new List<GameMessage>
        {
            new(player, new SystemMessageEvent($"Welcome, {player.Username}.")),
            new(player, new RoomDescriptionEvent(description.ToString()))
        };

        await rawMessageSender.SendLoginResultAsync(connectionId, true, $"Registered and logged in as {player.Username}.");

        _ = SendGameMessagesAsync(messages);
    }

    private async Task Login(ConnectionId connectionId, LoginCommand lc, string? sessionToken)
    {
        var dto = await playerStore.LoadPlayer(lc);

        if (dto is null)
        {
            await rawMessageSender.SendSystemMessageAsync(connectionId, "Login failed, please try again.");
            await rawMessageSender.SendLoginResultAsync(connectionId, false, "Login failed, please try again.");

            return;
        }

        var startingRoom = world.Rooms.TryGetValue(dto.CurrentLocation, out var r)
            ? r
            : world.Rooms.First()
                .Value;

        var player = new Player
        {
            Username = dto.Username,
            Connection = connectionFactory.Create(connectionId),
        };

        foreach (var item in dto.Inventory)
        {
            var obj = new Object
            {
                Id = new ObjectId(Guid.Parse(item.Id)),
                Name = item.Name,
                Description = item.Description
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
            new(player, new SystemMessageEvent($"Welcome back, {player.Username}.")),
            new(player, new RoomDescriptionEvent(description.ToString()))
        };

        await rawMessageSender.SendLoginResultAsync(connectionId, true, $"Logged in as {player.Username}.");

        _ = SendGameMessagesAsync(messages);
    }

    private void TrackSession(string? sessionToken, Player player, ConnectionId connectionId)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return;
        }

        _sessionPlayers[sessionToken] = player;
        _sessionConnections[sessionToken] = connectionId.Value;
    }
    
    private async Task SendGameMessagesAsync(List<GameMessage> messages, CancellationToken ct = default)
    {
        var tasks = messages
            .Select(msg => (msg.Player, Content: presenter.Present(msg)))
            .Where(msg => !string.IsNullOrWhiteSpace(msg.Content))
            .Select(msg => msg.Player.Connection.SendMessageAsync(msg.Content!, ct));

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending messages");
        }
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

    private Task RegisterAgent(ConnectionId connectionId, RegisterAgentCommand command)
    {
        var defaultRoom = world.Rooms.First()
            .Value;

        var player = new Player
        {
            Username = command.Identity.Name,
            Connection = command.Connection
        };

        world.MovePlayer(player, defaultRoom);

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

        await SendGameMessagesAsync(result.Messages, ct);
    }
}