using System.Threading.Channels;
using Microsoft.Extensions.Options;
using MooSharp;
using MooSharp.Agents;
using MooSharp.Messaging;

public sealed class AgentBrain : IAsyncDisposable
{
    private readonly AgentCore _core;
    private readonly IOptions<AgentOptions> _options;
    private readonly TimeProvider _clock;
    private readonly ChannelWriter<GameInput> _gameInputWriter;
    private readonly Channel<string> _incomingMessages;
    private readonly CancellationTokenSource _cts;

    // Fire-and-forget tasks
    private Task? _processingTask;
    private Task? _volitionTask;
    private readonly ConnectionId _myConnectionId;

    public AgentBrain(AgentCore core,
        AgentPlayerConnection connection,
        ChannelWriter<GameInput> gameInputWriter,
        CancellationToken ct,
        TimeProvider clock,
        IOptions<AgentOptions> options)
    {
        _core = core;
        _gameInputWriter = gameInputWriter;
        _clock = clock;
        _options = options;
        _myConnectionId = new(connection.Id);
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        // Wire up the connection to the internal channel
        _incomingMessages = Channel.CreateUnbounded<string>();
        connection.OnMessageReceived = async (msg) => await _incomingMessages.Writer.WriteAsync(msg, _cts.Token);
    }

    public async Task StartAsync()
    {
        await _core.InitializeAsync(_cts.Token);

        _processingTask = ProcessLoopAsync();
        _volitionTask = VolitionLoopAsync();
    }

    private async Task ProcessLoopAsync()
    {
        try
        {
            await foreach (var msg in _incomingMessages.Reader.ReadAllAsync(_cts.Token))
            {
                await foreach (var cmd in _core.ProcessMessageAsync(msg, _cts.Token))
                {
                    await DispatchCommands([cmd]);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task VolitionLoopAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10), _clock);

        try
        {
            while (await timer.WaitForNextTickAsync(_cts.Token))
            {
                var commands = await _core.ProcessVolitionAsync(_cts.Token);
                await DispatchCommands(commands);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task DispatchCommands(IEnumerable<InputCommand> commands)
    {
        foreach (var cmd in commands)
        {
            await _gameInputWriter.WriteAsync(new(_myConnectionId, cmd), _cts.Token);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();

        // ... (standard disposal logic)
    }
}