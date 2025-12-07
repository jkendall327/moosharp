using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using MooSharp.Game;

namespace MooSharp.Agents;

public class AgentCore(
    AgentCreationBundle bundle,
    IAgentPromptProvider promptProvider,
    IAgentResponseProvider responseProvider,
    TimeProvider clock,
    IOptions<AgentOptions> options,
    ILogger logger)
{
    private readonly AgentOptions _options = options.Value;

    private readonly ChatHistory _history = [];
    private DateTimeOffset _nextAllowedActionTime = DateTimeOffset.MinValue;
    private DateTimeOffset _lastActionTime = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _stateLock = new(1, 1);

    public async Task InitializeAsync(CancellationToken ct)
    {
        if (_history.Count > 0)
        {
            return;
        }

        var systemPrompt = await promptProvider.GetSystemPromptAsync(bundle.Name, bundle.Persona, ct);

        _history.AddSystemMessage(systemPrompt);
    }

    public async IAsyncEnumerable<InputCommand> ProcessMessageAsync(Guid actorId, string message,
        [EnumeratorCancellation] CancellationToken ct)
    {
        // We acquire the lock. Because this is an iterator, the lock remains 
        // held while the caller (AgentBrain) iterates through the loop.
        // It is released only when the enumeration finishes or is disposed.
        await _stateLock.WaitAsync(ct);

        try
        {
            _history.AddUserMessage(message);
            TrimHistory();

            // Check cooldown.
            if (!ShouldAct())
            {
                // Yield nothing and exit. 
                // The lock is released in the finally block.
                logger.LogDebug("Skipping agent turn due to action cooldown");
                yield break;
            }

            // Yield "Thinking" immediately so the UI can update.
            logger.LogDebug("Agent has begun thinking");

            yield return new(actorId, "/me is thinking...");

            // Perform the slow LLM call.
            // The shell is currently processing the Thinking command, 
            // but the lock prevents other messages from entering.
            var content = await responseProvider.GetResponse(bundle.Name, bundle.Source, _history, ct);

            var responseText = content.Content?.Trim();

            if (string.IsNullOrEmpty(responseText))
            {
                yield break;
            }
            
            logger.LogInformation("Got agent response: {AgentResponse}", responseText);

            _history.AddAssistantMessage(responseText);
            TrimHistory();

            // Yield the actual response.
            yield return new(actorId, responseText);

            _lastActionTime = clock.GetUtcNow();
        }
        finally
        {
            _stateLock.Release();
        }
    }

    public TimeSpan GetVolitionCooldown()
    {
        return bundle.VolitionCooldown;
    }

    public bool RequiresVolition()
    {
        var idle = clock
            .GetUtcNow()
            .Subtract(_lastActionTime);

        return idle > TimeSpan.FromMinutes(5) && ShouldAct();
    }

    private bool ShouldAct()
    {
        var now = clock.GetUtcNow();

        if (now < _nextAllowedActionTime)
        {
            return false;
        }

        _nextAllowedActionTime = now + bundle.ActionCooldown;

        return true;
    }

    private void TrimHistory()
    {
        var maxRecentMessages = Math.Max(0, _options.MaxRecentMessages);
        var maxHistorySize = maxRecentMessages + 1; // +1 for System Prompt

        if (_history.Count <= maxHistorySize)
        {
            return;
        }

        var messagesToRemove = _history.Count - maxHistorySize;

        // Ensure we don't remove the system prompt at index 0
        _history.RemoveRange(1, messagesToRemove);
    }
}