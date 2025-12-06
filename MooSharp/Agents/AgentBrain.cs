using System.Threading.Channels;
using Microsoft.Extensions.Options;
using MooSharp.Actors;
using MooSharp.Messaging;

namespace MooSharp.Agents;

public sealed class AgentBrain(
    AgentCore core,
    AgentPlayerConnection connection,
    ChannelWriter<GameInput> gameWriter,
    TimeProvider clock,
    IOptions<AgentOptions> options) : IAsyncDisposable
{
    private readonly Channel<string> _incomingMessages = Channel.CreateUnbounded<string>();
    private CancellationTokenSource? _cts;

    // Fire-and-forget tasks
    private Task? _processingTask;
    private Task? _volitionTask;

    public IPlayerConnection Connection { get; } = connection;

    public PlayerId Id { get; } = PlayerId.New();
    
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Wire up the connection to the internal channel
        connection.OnMessageReceived = WriteToInternalQueue;

        await core.InitializeAsync(_cts.Token);

        _processingTask = ProcessLoopAsync();
        _volitionTask = VolitionLoopAsync();
    }

    public async Task WriteToInternalQueue(string msg)
    {
        ArgumentNullException.ThrowIfNull(_cts);
        await _incomingMessages.Writer.WriteAsync(msg, _cts.Token);
    }

    private async Task ProcessLoopAsync()
    {
        ArgumentNullException.ThrowIfNull(_cts);

        try
        {
            await foreach (var msg in _incomingMessages.Reader.ReadAllAsync(_cts.Token))
            {
                await foreach (var cmd in core.ProcessMessageAsync(msg, _cts.Token))
                {
                    await gameWriter.WriteAsync(new(Connection.Id, cmd), _cts.Token);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task VolitionLoopAsync()
    {
        ArgumentNullException.ThrowIfNull(_cts);

        var cooldown = core.GetVolitionCooldown();

        using var timer = new PeriodicTimer(cooldown, clock);

        try
        {
            while (await timer.WaitForNextTickAsync(_cts.Token))
            {
                if (core.RequiresVolition())
                {
                    await WriteToInternalQueue(options.Value.VolitionPrompt);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        ArgumentNullException.ThrowIfNull(_cts);

        await _cts.CancelAsync();
        _cts.Dispose();

        _incomingMessages.Writer.Complete();

        if (_processingTask is not null)
        {
            await _processingTask;
        }

        if (_volitionTask is not null)
        {
            await _volitionTask;
        }
    }
}