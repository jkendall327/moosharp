using System.Text;
using Microsoft.AspNetCore.SignalR;
using MooSharp.Messaging;

namespace MooSharp;

using System.Threading.Channels;

public class GameEngine(
    World world,
    CommandParser parser,
    CommandExecutor executor,
    ChannelReader<GameInput> reader,
    IHubContext<MooHub> hubContext,
    ILogger<GameEngine> logger) : BackgroundService
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
        var player = world.Players.FirstOrDefault(p => p.ConnectionId == input.ConnectionId);

        if (player is null || input.Command is "LOGIN")
        {
            CreateNewPlayer(input.ConnectionId);
            return;
        }

        var command = await parser.ParseAsync(player, input.Command, ct);

        if (command is null)
        {
            _ = SendMessagesAsync([new(player, "That command wasn't recognised.")]);
            return;
        }

        try
        {
            var result = await executor.Handle(command, ct);

            var description = BuildCurrentRoomDescription(player).ToString();
            result.Messages.Add(new(player, description));
            
            _ = SendMessagesAsync(result.Messages);
        }
        catch (Exception)
        {
            _ = SendMessagesAsync([new(player, "An unexpected error occurred.")]);
        }
    }

    private void CreateNewPlayer(string connectionId)
    {
        var player = new Player
        {
            Username = Random
                .Shared
                .Next()
                .ToString(),
            
            ConnectionId = connectionId,
            
            CurrentLocation = world.Rooms.First().Value
        };

        world.Players.Add(player);

        var description = BuildCurrentRoomDescription(player);

        var messages = new List<GameMessage>
        {
            new(player, $"Welcome, {player.Username}."),
            new(player, description.ToString())
        };
        
        _ = SendMessagesAsync(messages);
    }

    private async Task SendMessagesAsync(List<GameMessage> messages)
    {
        var tasks = messages.Select(msg => hubContext
            .Clients
            .Client(msg.Player.ConnectionId)
            .SendAsync("ReceiveMessage", msg.Content));

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

        var players = room.PlayersInRoom;

        foreach (var playerActor in players)
        {
            if (playerActor == player)
            {
                continue;
            }

            sb.AppendLine($"{playerActor.Username} is here.");
        }

        var availableExits = player.CurrentLocation.Exits;

        var availableExitsMessage = $"Available exits: {string.Join(", ", availableExits.Select(s => s.Key))}";

        sb.AppendLine(availableExitsMessage);

        return sb;
    }
}