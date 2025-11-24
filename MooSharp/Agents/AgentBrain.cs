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
using System.Text;
using System.Threading;
using System.Threading.Channels;

namespace MooSharp.Agents;

public class AgentBrain : IAsyncDisposable
{
    private readonly AgentPlayerConnection _connection;
    private readonly ChannelWriter<GameInput> _gameInputWriter;
    private readonly ChatHistory _history;
    private readonly IOptions<AgentOptions> _options;
    private readonly TimeProvider _clock;
    private readonly string _persona;
    private readonly AgentSource _source;
    private readonly string _availableCommands;

    private readonly Channel<string> _incomingMessages;
    private readonly CancellationTokenSource _cts;
    private readonly Task _processingTask;

    // Rate limiting
    private readonly TimeSpan _actionCooldown;
    private DateTimeOffset _nextAllowedActionTime = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _cooldownLock = new(1, 1);

    public AgentBrain(string name,
        string persona,
        AgentSource source,
        string availableCommands,
        ChannelWriter<GameInput> gameInputWriter,
        IOptions<AgentOptions> options,
        TimeProvider clock,
        TimeSpan? actionCooldown = null,
        CancellationToken cancellationToken = default)
    {
        _persona = persona;
        _source = source;
        _availableCommands = availableCommands;
        _gameInputWriter = gameInputWriter;
        _options = options;
        _clock = clock;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _actionCooldown = actionCooldown ?? TimeSpan.FromSeconds(10);

        _incomingMessages = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        _connection = new()
        {
            // When the Game Engine sends text to the Agent, this triggers:
            OnMessageReceived = EnqueueIncomingMessageAsync
        };

        _processingTask = Task.Run(() => ProcessIncomingMessagesAsync(_cts.Token));

        var systemPrompt = new StringBuilder()
            .AppendLine($"You are a player in a text-based adventure game. Your name is {name}.")
            .AppendLine(persona)
            .AppendLine("Use only the commands listed below, and respond with a single command starting with the command verb.")
            .AppendLine(_availableCommands)
            .ToString();

        _history = new(systemPrompt);
    }

    public IPlayerConnection Connection => _connection;

    private Task EnqueueIncomingMessageAsync(string message)
    {
        if (!_incomingMessages.Writer.TryWrite(message))
        {
            return _incomingMessages.Writer.WriteAsync(message).AsTask();
        }

        return Task.CompletedTask;
    }

    private async Task ProcessIncomingMessagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (await _incomingMessages.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (_incomingMessages.Reader.TryRead(out var message))
                {
                    try
                    {
                        await HandleIncomingGameMessageAsync(message).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch
                    {
                        // Swallow exceptions to keep the processing loop alive
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during disposal
        }
    }

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
                // Still on cooldown - no action
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

    private async Task HandleIncomingGameMessageAsync(string message)
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

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _incomingMessages.Writer.TryComplete();

        try
        {
            await _processingTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested)
        {
            // Expected during disposal
        }

        _cts.Dispose();
    }
}
