using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using MooSharp.Messaging;
using System.Threading.Channels;

namespace MooSharp.Agents;

public class AgentBrain
{
    private readonly AgentPlayerConnection _connection;
    private readonly ChannelWriter<GameInput> _gameInputWriter;
    private readonly ChatHistory _history;
    private readonly IOptions<AgentOptions> _options;
    private readonly TimeProvider _clock;
    private readonly string _persona;
    private readonly AgentSource _source;

    // Rate limiting
    private readonly TimeSpan _actionCooldown;
    private DateTimeOffset _nextAllowedActionTime = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _cooldownLock = new(1, 1);

    public AgentBrain(string name,
        string persona,
        AgentSource source,
        ChannelWriter<GameInput> gameInputWriter,
        IOptions<AgentOptions> options,
        TimeProvider clock,
        TimeSpan? actionCooldown = null)
    {
        _persona = persona;
        _source = source;
        _gameInputWriter = gameInputWriter;
        _options = options;
        _clock = clock;

        _actionCooldown = actionCooldown ?? TimeSpan.FromSeconds(10);

        _connection = new()
        {
            // When the Game Engine sends text to the Agent, this triggers:
            OnMessageReceived = HandleIncomingGameMessage
        };

        _history = new($"You are a player in a text-based adventure game. Your name is {name}. {persona}");
    }

    public IPlayerConnection Connection => _connection;

    private async Task<bool> ShouldActAsync()
    {
        var now = _clock.GetUtcNow();

        await _cooldownLock
            .WaitAsync()
            .ConfigureAwait(false);

        try
        {
            if (now < _nextAllowedActionTime)
            {
                // Still on cooldown – no action
                return false;
            }

            // We’re allowed to act now; set the next allowed time
            _nextAllowedActionTime = now + _actionCooldown;

            return true;
        }
        finally
        {
            _cooldownLock.Release();
        }
    }

    private async Task HandleIncomingGameMessage(string message)
    {
        // Always record history.
        _history.AddUserMessage(message);
        TrimHistory();

        // Only sometimes actually act, based on cooldown
        if (!await ShouldActAsync())
        {
            return;
        }

        var kernel = await GetResponse();
        var commandText = kernel.Content?.Trim();

        if (string.IsNullOrEmpty(commandText))
        {
            return;
        }

        _history.AddAssistantMessage(commandText);
        TrimHistory();

        var id = new ConnectionId(_connection.Id);

        var command = new WorldCommand
        {
            Command = commandText
        };

        await _gameInputWriter.WriteAsync(new(id, command));
    }

    private async Task<ChatMessageContent> GetResponse()
    {
        var o = _options.Value;
        return _source switch
        {
            AgentSource.OpenAI => await GetOpenAIResponseAsync(o.OpenAIModelId, o.OpenAIApiKey),
            AgentSource.OpenRouter => await GetOpenAIResponseAsync(o.OpenRouterModelId, o.OpenRouterApiKey, o.OpenRouterEndpoint),
            AgentSource.Gemini => await GetGeminiResponseAsync(o),
            AgentSource.Anthropic => await GetAnthropicResponseAsync(o),
            _ => throw new NotSupportedException($"Unknown agent source {_source}")
        };
    }

    private void TrimHistory()
    {
        var maxRecentMessages = Math.Max(0, _options.Value.MaxRecentMessages);

        // Always reserve one slot for the system prompt at index 0.
        var maxHistorySize = maxRecentMessages + 1;

        if (_history.Count <= maxHistorySize)
        {
            return;
        }

        var messagesToRemove = _history.Count - maxHistorySize;
        _history.RemoveRange(1, messagesToRemove);
    }

    private async Task<ChatMessageContent> GetOpenAIResponseAsync(string modelId, string apiKey, string? endpoint = null)
    {
        var builder = Kernel.CreateBuilder();

        if (endpoint is null)
        {
            builder.AddOpenAIChatCompletion(modelId, apiKey);
        }
        else
        {
            builder.AddOpenAIChatCompletion(modelId, new Uri(endpoint), apiKey);
        }

        var kernel = builder.Build();

        var chat = kernel.Services.GetRequiredService<IChatCompletionService>();

        return await chat.GetChatMessageContentAsync(_history,
            executionSettings: new OpenAIPromptExecutionSettings
            {
                MaxTokens = 500
            },
            kernel: kernel);
    }

    private async Task<ChatMessageContent> GetGeminiResponseAsync(AgentOptions options)
    {
        var kernel = Kernel
            .CreateBuilder()
            .AddGoogleAIGeminiChatCompletion(options.GeminiModelId, options.GeminiApiKey)
            .Build();

        var chat = kernel.Services.GetRequiredService<IChatCompletionService>();

        return await chat.GetChatMessageContentAsync(
            _history,
            executionSettings: new GeminiPromptExecutionSettings
            {
                MaxTokens = 500
            },
            kernel: kernel);
    }

    private async Task<ChatMessageContent> GetAnthropicResponseAsync(AgentOptions options)
    {
        using var client = new AnthropicClient(new APIAuthentication(apiKey: options.AnthropicApiKey));
        var chatClient = (IChatClient)client.Messages;

        var messages = _history
            .Select(ConvertToChatMessage)
            .ToList();

        var response = await chatClient.GetResponseAsync(
            messages,
            new ChatOptions
            {
                ModelId = options.AnthropicModelId,
                MaxOutputTokens = 500
            },
            CancellationToken.None);

        return new ChatMessageContent(AuthorRole.Assistant, response.Text ?? string.Empty);
    }

    private static ChatMessage ConvertToChatMessage(ChatMessageContent message)
    {
        return message.Role switch
        {
            var role when role == AuthorRole.User => new ChatMessage(ChatRole.User, message.Content ?? string.Empty),
            var role when role == AuthorRole.Assistant => new ChatMessage(ChatRole.Assistant, message.Content ?? string.Empty),
            var role when role == AuthorRole.System => new ChatMessage(ChatRole.System, message.Content ?? string.Empty),
            _ => new ChatMessage(ChatRole.User, message.Content ?? string.Empty)
        };
    }
}
