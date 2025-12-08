using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
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
    private static readonly string[] ThinkingEmotes = [
        "/me is thinking...", "/me frowns in thought.", "/me pauses for a moment.",
        "/me seems to be processing that."
    ];

    private readonly AgentOptions _options = options.Value;

    private readonly ChatHistory _history = [];
    private DateTimeOffset _nextAllowedActionTime = DateTimeOffset.MinValue;
    private DateTimeOffset _lastActionTime = DateTimeOffset.MinValue;

    public async Task InitializeAsync(CancellationToken ct)
    {
        if (_history.Count > 0)
        {
            return;
        }

        var systemPrompt = await promptProvider.GetSystemPromptAsync(bundle.Name, bundle.Persona, ct);

        _history.AddSystemMessage(systemPrompt);
    }

    public async IAsyncEnumerable<InputCommand> ProcessMessageAsync(Guid actorId,
        string message,
        [EnumeratorCancellation] CancellationToken ct)
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

        logger.LogDebug("Agent has begun thinking");

        var llmTask = responseProvider.GetResponse(bundle.Name, bundle.Source, _history, ct);

        // Create a "Patience Task". If this wins, we emit the thinking emote.
        // 2 seconds is usually the "awkward silence" threshold in text chat.
        var patienceTask = Task.Delay(TimeSpan.FromSeconds(2), ct);

        var completedTask = await Task.WhenAny(llmTask, patienceTask);

        // 4. Did we run out of patience?
        if (completedTask == patienceTask)
        {
            logger.LogDebug("Agent is taking a while; emitting thinking emote");

            yield return new(actorId, GetThinkingEmote());
        }

        ChatMessageContent content;

        try
        {
            content = await llmTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LLM generation failed");

            yield break;
        }

        var responseText = content.Content?.Trim();

        if (string.IsNullOrEmpty(responseText))
        {
            yield break;
        }

        // AIs are loathe to not return responses, so we give them an explicit option for skipping.
        if (string.Equals(responseText, "<skip>"))
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

    private string GetThinkingEmote()
    {
        return ThinkingEmotes[Random.Shared.Next(ThinkingEmotes.Length)];
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