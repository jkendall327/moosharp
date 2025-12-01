using System.Diagnostics;
using Anthropic.SDK;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace MooSharp.Agents;

public interface IAgentResponseProvider
{
    Task<ChatMessageContent> GetResponse(string name, AgentSource source, ChatHistory history, CancellationToken ct = default);
}

public class AgentResponseProvider(
    IAgentPromptProvider promptProvider,
    IOptions<AgentOptions> options,
    ILogger<AgentResponseProvider> logger) : IAgentResponseProvider
{
    public async Task<ChatMessageContent> GetResponse(string name, AgentSource source, ChatHistory history, CancellationToken ct = default)
    {
        /*
         * We create a new kernel in each of these branches because setting up different AI providers in
         * DI for Semantic Kernel is a real pain.
         * This is fine because making new kernels is cheap.
         * Official Microsoft statement:
         * 'We recommend that you create a kernel as a transient service so that it is disposed of after each use because the plugin collection is mutable.
         * The kernel is extremely lightweight (since it's just a container for services and plugins),
         * so creating a new kernel for each use is not a performance concern.'
         * https://learn.microsoft.com/en-us/semantic-kernel/concepts/kernel?pivots=programming-language-csharp
         */

        logger.LogInformation("Agent {AgentName} starting LLM call via {Source}", name, source);

        var stopwatch = Stopwatch.StartNew();

        logger.LogDebug("Requesting response for {AgentName} using {Source}", name, source);

        var content = source switch
        {
            AgentSource.OpenAI => await GetOpenAiResponseAsync(history),
            AgentSource.OpenRouter => await GetOpenAiResponseAsync(history, endpoint: options.Value.OpenRouterEndpoint),
            AgentSource.Gemini => await GetGeminiResponseAsync(history),
            AgentSource.Anthropic => await GetAnthropicResponseAsync(history),
            var _ => throw new NotSupportedException($"Unknown agent source {source}")
        };

        stopwatch.Stop();

        logger.LogInformation("Agent {AgentName} completed LLM call in {ElapsedMilliseconds} ms",
            name,
            stopwatch.ElapsedMilliseconds);

        return content;
    }

    private async Task<ChatMessageContent> GetOpenAiResponseAsync(ChatHistory currentHistory, string? endpoint = null)
    {
        var builder = Kernel.CreateBuilder();

        var o = options.Value;

        if (endpoint is null)
        {
            builder.AddOpenAIChatCompletion(o.OpenAIModelId, o.OpenAIApiKey);
        }
        else
        {
            builder.AddOpenAIChatCompletion(o.OpenRouterModelId, new Uri(endpoint), o.OpenRouterApiKey);
        }

        var kernel = builder.Build();

        var chat = kernel.Services.GetRequiredService<IChatCompletionService>();

        var history = await promptProvider
            .PrepareHistoryAsync(currentHistory)
            .ConfigureAwait(false);

        return await chat.GetChatMessageContentAsync(history,
            executionSettings: new OpenAIPromptExecutionSettings
            {
                MaxTokens = 500
            },
            kernel: kernel);
    }

    private async Task<ChatMessageContent> GetGeminiResponseAsync(ChatHistory currentHistory)
    {
        var kernel = Kernel
            .CreateBuilder()
            .AddGoogleAIGeminiChatCompletion(options.Value.GeminiModelId, options.Value.GeminiApiKey)
            .Build();

        var chat = kernel.Services.GetRequiredService<IChatCompletionService>();

        var history = await promptProvider
            .PrepareHistoryAsync(currentHistory)
            .ConfigureAwait(false);

        return await chat.GetChatMessageContentAsync(history,
            executionSettings: new GeminiPromptExecutionSettings
            {
                MaxTokens = 500
            },
            kernel: kernel);
    }

    private async Task<ChatMessageContent> GetAnthropicResponseAsync(ChatHistory currentHistory)
    {
        using var client = new AnthropicClient(new(apiKey: options.Value.AnthropicApiKey));
        IChatClient? chatClient = client.Messages;

        var history = await promptProvider
            .PrepareHistoryAsync(currentHistory)
            .ConfigureAwait(false);

        var messages = history
            .Select(ConvertToChatMessage)
            .ToList();

        var response = await chatClient.GetResponseAsync(messages,
            new()
            {
                ModelId = options.Value.AnthropicModelId,
                MaxOutputTokens = 500
            },
            CancellationToken.None);

        return new(AuthorRole.Assistant, response.Text);
    }

    private static ChatMessage ConvertToChatMessage(ChatMessageContent message)
    {
        return message.Role switch
        {
            var role when role == AuthorRole.User => new ChatMessage(ChatRole.User, message.Content ?? string.Empty),
            var role when role == AuthorRole.Assistant => new ChatMessage(ChatRole.Assistant,
                message.Content ?? string.Empty),
            var role when role == AuthorRole.System =>
                new ChatMessage(ChatRole.System, message.Content ?? string.Empty),
            var _ => new ChatMessage(ChatRole.User, message.Content ?? string.Empty)
        };
    }
}