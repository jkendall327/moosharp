using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MooSharp.Actors.Players;
using MooSharp.Game;

namespace MooSharp.Agents;

public sealed class AgentBrain(
    AgentCore core,
    ChannelWriter<GameCommand> gameWriter,
    TimeProvider clock,
    IOptions<AgentOptions> options,
    ILogger logger)
{
    private readonly Channel<string> _inbox = Channel.CreateUnbounded<string>();

    private PlayerId Id { get; set; }

    public async Task RunAsync(Guid id, CancellationToken ct)
    {
        Id = new(id);

        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            { "AgentId", id }
        });

        logger.LogDebug("Agent brain initializing");
        await core.InitializeAsync(ct);
        logger.LogDebug("Agent brain initialized, entering main loop");

        // Volition logic
        var volitionInterval = core.GetVolitionCooldown();
        var lastActionTime = clock.GetUtcNow();

        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Calculate how long until we get bored
                var timeSinceLastAction = clock.GetUtcNow() - lastActionTime;
                var timeUntilBored = volitionInterval - timeSinceLastAction;

                // If we are already bored, set delay to 0
                if (timeUntilBored < TimeSpan.Zero) timeUntilBored = TimeSpan.Zero;

                // Create a task that completes when a message arrives
                var readTask = _inbox
                    .Reader
                    .WaitToReadAsync(ct)
                    .AsTask();

                // Create a task that completes when we get bored
                var boredomTask = Task.Delay(timeUntilBored, ct);

                // WAIT for either: A message arrives OR We get bored
                var completedTask = await Task.WhenAny(readTask, boredomTask);

                if (completedTask == readTask)
                {
                    // We have messages! Process all available.
                    logger.LogDebug("Agent received message, processing events");
                    await ReactToEvents(ct);
                }
                else
                {
                    // We timed out. The agent is bored.
                    if (!core.RequiresVolition())
                    {
                        continue;
                    }

                    logger.LogDebug("Agent triggered volition (idle timeout)");

                    // Generate a thought/action based on the volition prompt
                    var prompt = options.Value.VolitionPrompt;

                    await foreach (var cmd in core.ProcessMessageAsync(Id.Value, prompt, ct))
                    {
                        await gameWriter.WriteAsync(cmd, ct);
                    }
                }

                lastActionTime = clock.GetUtcNow();
            }
            catch (OperationCanceledException)
            {
                logger.LogDebug("Agent loop cancelled");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Agent brain error during main loop");
            }
        }

        logger.LogDebug("Agent brain exited main loop");
    }

    private async Task ReactToEvents(CancellationToken ct)
    {
        while (_inbox.Reader.TryRead(out var msg))
        {
            await foreach (var cmd in core.ProcessMessageAsync(Id.Value, msg, ct))
            {
                await gameWriter.WriteAsync(cmd, ct);
            }
        }
    }

    public ValueTask EnqueueMessageAsync(string message, CancellationToken ct)
    {
        return _inbox.Writer.WriteAsync(message, ct);
    }
}