using System.Runtime.CompilerServices;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MooSharp.Agents;

public class AgentCore
{
    private readonly AgentCreationBundle _bundle;
    private readonly IAgentPromptProvider _promptProvider;
    private readonly IAgentResponseProvider _responseProvider;
    private readonly TimeProvider _clock;
    private readonly AgentOptions _options;
    private readonly ILogger _logger;

    // State
    private readonly ChatHistory _history = [];
    private DateTimeOffset _nextAllowedActionTime = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _stateLock = new(1, 1);

    public AgentCore(AgentCreationBundle bundle,
        IAgentPromptProvider promptProvider,
        IAgentResponseProvider responseProvider,
        TimeProvider clock,
        IOptions<AgentOptions> options,
        ILogger logger)
    {
        _bundle = bundle;
        _promptProvider = promptProvider;
        _responseProvider = responseProvider;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken ct)
    {
        // Logic extracted from EnsureSystemPromptAsync
        if (_history.Count > 0) return;

        var systemPrompt = await _promptProvider.GetSystemPromptAsync(_bundle.Name, _bundle.Persona, ct);

        _history.AddSystemMessage(systemPrompt);
    }

    public async IAsyncEnumerable<InputCommand> ProcessMessageAsync(
        string message, 
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

            // 1. Check Cooldown
            if (!ShouldAct())
            {
                // Yield nothing and exit. 
                // The lock is released in the finally block.
                yield break; 
            }

            // 2. Yield "Thinking" immediately so the UI can update.
            yield return new AgentThinkingCommand();

            // 3. Perform the slow LLM call
            // The shell is currently processing the Thinking command, 
            // but the lock prevents other messages from entering.
            var content = await _responseProvider.GetResponse(
                _bundle.Name, 
                _bundle.Source, 
                _history,
                ct);
                
            var responseText = content.Content?.Trim();

            if (!string.IsNullOrEmpty(responseText))
            {
                _history.AddAssistantMessage(responseText);
                TrimHistory();

                // 4. Yield the actual "Response"
                yield return new WorldCommand { Command = responseText };
            }
        }
        finally
        {
            _stateLock.Release();
        }
    }

    public async Task<IReadOnlyList<InputCommand>> ProcessVolitionAsync(CancellationToken ct)
    {
        await _stateLock.WaitAsync(ct);

        try
        {
            if (!ShouldAct())
            {
                return [];
            }

            // For volition, we might inject a specific prompt into history, or just ask the LLM
            _history.AddUserMessage(_options.VolitionPrompt);
            TrimHistory();

            return await GenerateResponseAsync(ct);
        }
        finally
        {
            _stateLock.Release();
        }
    }

    private bool ShouldAct()
    {
        var now = _clock.GetUtcNow();

        if (now < _nextAllowedActionTime)
        {
            return false;
        }

        _nextAllowedActionTime = now + _bundle.ActionCooldown;

        return true;
    }

    private async Task<IReadOnlyList<InputCommand>> GenerateResponseAsync(CancellationToken ct)
    {
        var outputs = new List<InputCommand>
        {
            new AgentThinkingCommand()
        };

        var content = await _responseProvider.GetResponse(_bundle.Name, _bundle.Source, _history, ct);
        var responseText = content.Content?.Trim();

        if (string.IsNullOrEmpty(responseText))
        {
            return outputs;
        }

        _history.AddAssistantMessage(responseText);
        TrimHistory();

        outputs.Add(new WorldCommand
        {
            Command = responseText
        });

        return outputs;
    }

    private void TrimHistory()
    {
        var maxRecentMessages = Math.Max(0, _options.MaxRecentMessages);
        var maxHistorySize = maxRecentMessages + 1; // +1 for System Prompt

        if (_history.Count <= maxHistorySize) return;

        var messagesToRemove = _history.Count - maxHistorySize;

        // Ensure we don't remove the system prompt at index 0
        _history.RemoveRange(1, messagesToRemove);
    }
}