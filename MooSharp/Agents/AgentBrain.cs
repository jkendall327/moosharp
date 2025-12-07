using System.Threading.Channels;
using Microsoft.Extensions.Options;
using MooSharp.Actors;
using MooSharp.Messaging;

namespace MooSharp.Agents;

public sealed class AgentBrain(
    AgentCore core,
    ChannelWriter<GameCommand> gameWriter,
    TimeProvider clock,
    IOptions<AgentOptions> options) : IAsyncDisposable
{
    private readonly Channel<string> _incomingMessages = Channel.CreateUnbounded<string>();
    private CancellationTokenSource? _cts;

    // Fire-and-forget tasks
    private Task? _processingTask;
    private Task? _volitionTask;
    
    public PlayerId Id { get; private set; }
    
    public async Task StartAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Id = new(id);
        
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
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
                await foreach (var cmd in core.ProcessMessageAsync(Id.Value, msg, _cts.Token))
                {
                    await gameWriter.WriteAsync(cmd, _cts.Token);
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