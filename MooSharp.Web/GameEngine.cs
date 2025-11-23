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
            case WorldCommand wc:
                var player = world.Players[input.ConnectionId.Value];
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
        if (!world.Players.TryGetValue(connectionId.Value, out var player))
        {
            logger.LogWarning("Player with connection {ConnectionId} not found during disconnect", connectionId);

            return;
        }

        await playerStore.SavePlayer(player);

        player.CurrentLocation.PlayersInRoom.Remove(player);

        world.Players.Remove(connectionId.Value);

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

            var description = BuildCurrentRoomDescription(player)
                .ToString();

            result.Messages.Add(new(player, new RoomDescriptionEvent(description)));

            _ = SendMessagesAsync(result.Messages);
        }
        catch (Exception)
        {
            _ = SendMessagesAsync([new(player, new SystemMessageEvent("An unexpected error occurred."))]);
        }
    }

    private async Task CreateNewPlayer(ConnectionId connectionId, RegisterCommand rc)
    {
        var defaultRoom = world.Rooms.First()
            .Value;

        var player = new Player
        {
            Username = rc.Username,
            Connection = new SignalRPlayerConnection(connectionId, hubContext),
            CurrentLocation = defaultRoom
        };

        await playerStore.SaveNewPlayer(player, rc.Password);

        world.Players.Add(connectionId.Value, player);

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
        
        var startingRoom = world.Rooms.TryGetValue(dto.CurrentLocation, out var r) ? r : world.Rooms.First().Value;
        
        // TODO: inventory?
        
        var player = new Player
        {
            Username = dto.Username,
            Connection = new SignalRPlayerConnection(connectionId, hubContext),
            CurrentLocation = startingRoom,
        };
        
        world.Players.Add(connectionId.Value, player);

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

        var room = player.CurrentLocation;

        sb.AppendLine(room.Description);

        var otherPlayers = room.PlayersInRoom.Select(s => s.Username).Except([player.Username]);

        sb.AppendLine($"{string.Join(", ", otherPlayers)} are here.");

        var availableExits = player.CurrentLocation.Exits.Select(s => s.Key);

        var availableExitsMessage = $"Available exits: {string.Join(", ", availableExits)}";

        sb.AppendLine(availableExitsMessage);

        return sb;
    }
}