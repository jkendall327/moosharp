using System.Text;
using Microsoft.AspNetCore.SignalR;
using MooSharp.Messaging;
using MooSharp.Persistence;

namespace MooSharp;

using System.Threading.Channels;

public class GameEngine(
    World world,
    CommandParser parser,
    CommandExecutor executor,
    ChannelReader<GameInput> reader,
    IPlayerStore playerStore,
    IHubContext<MooHub> hubContext,
    ILogger<GameEngine> logger,
    IGameMessagePresenter presenter) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var input in reader.ReadAllAsync(stoppingToken))
        {
            await ProcessInput(input, stoppingToken);
        }
    }

    private async Task ProcessInput(GameInput input, CancellationToken ct = default)
    {
        switch (input.Command)
        {
            case RegisterCommand rc: await CreateNewPlayer(input.ConnectionId, rc); break;
            case LoginCommand lc: await Login(input.ConnectionId, lc); break;
            case RegisterAgentCommand ra: await RegisterAgent(input.ConnectionId, ra); break;
            case WorldCommand wc:
                if (!world.TryGetPlayer(input.ConnectionId, out var player))
                {
                    logger.LogWarning("Received world command for unknown connection {ConnectionId}", input.ConnectionId);

                    return;
                }
                await ProcessWorldCommand(wc, ct, player);

                break;
            case DisconnectCommand:
                await HandleDisconnectAsync(input.ConnectionId);

                break;
            default: throw new ArgumentOutOfRangeException(nameof(input.Command));
        }
    }

    private async Task HandleDisconnectAsync(ConnectionId connectionId)
    {
        if (!world.TryGetPlayer(connectionId, out var player))
        {
            logger.LogWarning("Player with connection {ConnectionId} not found during disconnect", connectionId);

            return;
        }

        var location = world.GetPlayerLocation(player);

        if (location is null)
        {
            logger.LogWarning("Player {Player} has no known location during disconnect.", player.Username);

            location = world.GetDefaultRoom();

            world.MovePlayer(player, location);
        }

        await playerStore.SavePlayer(player, location);
        location.PlayersInRoom.Remove(player);

        world.RemovePlayer(connectionId);

        logger.LogInformation("Player {Player} disconnected", player.Username);

        return;
    }

    private async Task ProcessWorldCommand(WorldCommand command, CancellationToken ct, Player player)
    {
        var parsed = await parser.ParseAsync(player, command.Command, ct);

        if (parsed is null)
        {
            _ = SendMessagesAsync([new(player, new SystemMessageEvent("That command wasn't recognised."))]);

            return;
        }

        try
        {
            var result = await executor.Handle(parsed, ct);

            _ = SendMessagesAsync(result.Messages);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing world command {Command}", command.Command);
            _ = SendMessagesAsync([new(player, new SystemMessageEvent("An unexpected error occurred."))]);
        }
    }

    private async Task CreateNewPlayer(ConnectionId connectionId, RegisterCommand rc)
    {
        var defaultRoom = world.GetDefaultRoom();

        var player = new Player
        {
            Username = rc.Username,
            Connection = new SignalRPlayerConnection(connectionId, hubContext)
        };

        world.MovePlayer(player, defaultRoom);

        await playerStore.SaveNewPlayer(player, defaultRoom, rc.Password);

        world.AddPlayer(connectionId, player);

        var description = BuildCurrentRoomDescription(player);

        var messages = new List<GameMessage>
        {
            new(player, new SystemMessageEvent($"Welcome, {player.Username}.")),
            new(player, new RoomDescriptionEvent(description.ToString()))
        };

        _ = SendMessagesAsync(messages);
    }

    private async Task Login(ConnectionId connectionId, LoginCommand lc)
    {
        var dto = await playerStore.LoadPlayer(lc);

        if (dto is null)
        {
            await hubContext
                .Clients
                .Client(connectionId.Value)
                .SendAsync("ReceiveMessage", "Login failed, please try again.");

            return;
        }
        
        var startingRoom = world.TryGetRoom(dto.CurrentLocation, out var r) ? r : world.GetDefaultRoom();
        
        // TODO: inventory?
        
        var player = new Player
        {
            Username = dto.Username,
            Connection = new SignalRPlayerConnection(connectionId, hubContext),
        };

        world.MovePlayer(player, startingRoom);

        world.AddPlayer(connectionId, player);

        var description = BuildCurrentRoomDescription(player);

        var messages = new List<GameMessage>
        {
            new(player, new SystemMessageEvent($"Welcome back, {player.Username}.")),
            new(player, new RoomDescriptionEvent(description.ToString()))
        };

        _ = SendMessagesAsync(messages);
    }

    private async Task SendMessagesAsync(List<GameMessage> messages)
    {
        var tasks = messages
            .Select(msg => (msg.Player, Content: presenter.Present(msg)))
            .Where(msg => !string.IsNullOrWhiteSpace(msg.Content))
            .Select(msg => msg.Player.Connection.SendMessageAsync(msg.Content));

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
            logger.LogWarning("Player {Player} has no known location when building description.", player.Username);

            sb.AppendLine("You are nowhere.");

            return sb;
        }

        sb.Append(room.DescribeFor(player));

        return sb;
    }

    private Task RegisterAgent(ConnectionId connectionId, RegisterAgentCommand command)
    {
        var defaultRoom = world.GetDefaultRoom();

        var player = new Player
        {
            Username = command.Identity.Name,
            Connection = command.Connection
        };

        world.MovePlayer(player, defaultRoom);

        world.AddPlayer(connectionId, player);

        logger.LogInformation("Agent {AgentName} registered", player.Username);

        return Task.CompletedTask;
    }
}