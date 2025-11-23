using System.Text;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using MooSharp.Messaging;

namespace MooSharp;

using System.Threading.Channels;

public record GameInput(string ConnectionId, string Command);

public class GameEngine(World world, CommandParser parser, CommandExecutor executor, IHubContext<MooHub> hubContext)
    : BackgroundService
{
    private readonly Dictionary<string, Player> _playerConnections = new();
    
    private readonly Channel<GameInput> _inputQueue = Channel.CreateUnbounded<GameInput>();

    public void EnqueueInput(string connectionId, string command)
    {
        _inputQueue.Writer.TryWrite(new GameInput(connectionId, command));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var input in _inputQueue.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessInput(input, stoppingToken);
        }
    }

    private async Task ProcessInput(GameInput input, CancellationToken ct = default)
    {
        // var player = _world.Players.Values
        //     .FirstOrDefault(p => p.ConnectionId == input.ConnectionId);

        var player = new Player()
        {
            Username = "fake",
            CurrentLocation = null!
        };

        if (player == null) return; // Or handle login logic

        var command = await parser.ParseAsync(player, input.Command, ct);

        if (command is null)
        {
            _ = SendMessagesAsync([new GameMessage(player, "That command wasn't recognised.")]);
            return;
        }
        
        try
        {
            var result = await executor.Handle(command, ct);
            _ = SendMessagesAsync(result.Messages);
        }
        catch (Exception)
        {
            _ = SendMessagesAsync([new(player, "An unexpected error occurred.")]);
        }
    }

    private async Task SendMessagesAsync(List<GameMessage> messages)
    {
        // map between players and connections here...
        var tasks = messages.Select(msg => hubContext
            .Clients
            .Client(msg.TargetConnectionId)
            .SendAsync("ReceiveMessage", msg.Content));

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            // Log error, but don't crash the game
        }
    }
    
    private void BuildCurrentRoomDescription(Player player, StringBuilder sb)
    {
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
    }
}