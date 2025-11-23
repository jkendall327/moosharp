using System.Text;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;

namespace MooSharp;

using System.Threading.Channels;

public record GameInput(string ConnectionId, string Command);

public class GameEngine(World world, CommandParser parser, CommandExecutor executor, IHubContext<MooHub> hubContext)
    : BackgroundService
{
    private readonly Channel<GameInput> _inputQueue = Channel.CreateUnbounded<GameInput>();

    public void EnqueueInput(string connectionId, string command)
    {
        _inputQueue.Writer.TryWrite(new GameInput(connectionId, command));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var input in _inputQueue.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessInput(input);
        }
    }

    private async Task ProcessInput(GameInput input)
    {
        // var player = _world.Players.Values
        //     .FirstOrDefault(p => p.ConnectionId == input.ConnectionId);

        var player = new PlayerActor(null!, new NullLoggerFactory());

        if (player == null) return; // Or handle login logic

        var command = await parser.ParseAsync(player, input.Command);

        var outputBuffer = new StringBuilder();

        try
        {
            await executor.Handle(command, outputBuffer);
        }
        catch (Exception ex)
        {
            outputBuffer.AppendLine("Something went wrong.");
        }

        await hubContext
            .Clients
            .Client(input.ConnectionId)
            .SendAsync("ReceiveMessage", outputBuffer.ToString());
    }
}