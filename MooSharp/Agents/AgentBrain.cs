using Anthropic.SDK;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using MooSharp.Messaging;
using System.Text;
using System.Threading.Channels;

namespace MooSharp.Agents;

public sealed class AgentBrain : IAsyncDisposable
{
    private readonly AgentPlayerConnection _connection;
    private readonly ChannelWriter<GameInput> _gameInputWriter;
    private readonly ChatHistory _history;
    private readonly ILogger _logger;
    private readonly string _name;
    private readonly IOptions<AgentOptions> _options;
    private readonly TimeProvider _clock;
    private readonly AgentSource _source;

    private readonly Channel<string> _incomingMessages;
    private readonly CancellationTokenSource _cts;
    private Task? _processingTask;

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
        ILogger logger,
        TimeSpan? actionCooldown = null,
        CancellationToken cancellationToken = default)
    {
        _name = name;
        _source = source;
        _gameInputWriter = gameInputWriter;
        _logger = logger;
        _options = options;
        _clock = clock;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _actionCooldown = actionCooldown ?? TimeSpan.FromSeconds(10);

        _incomingMessages = Channel.CreateUnbounded<string>(new()
        {
            SingleReader = true,
            SingleWriter = false
        });

        _connection = new()
        {
            // When the Game Engine sends text to the Agent, this triggers:
            OnMessageReceived = EnqueueIncomingMessageAsync
        };

        var systemPrompt = new StringBuilder()
            .AppendLine($"You are a player in a text-based adventure game. Your name is {name}.")
            .AppendLine(persona)
            .AppendLine(
                "Use only the commands listed below, and respond with a single command starting with the command verb.")
            .AppendLine(availableCommands)
            .ToString();

        _history = new(systemPrompt);
    }

    public IPlayerConnection Connection => _connection;

    public void Start(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting agent brain for {AgentName} (source: {AgentSource})", _name, _source);
        _processingTask = Task.Run(() => ProcessIncomingMessagesAsync(_cts.Token), ct);
    }

    private Task EnqueueIncomingMessageAsync(string message)
    {
        _logger.LogDebug("Queuing incoming message for {AgentName}: {Message}", _name, message);

        if (!_incomingMessages.Writer.TryWrite(message))
        {
            return _incomingMessages
                .Writer
                .WriteAsync(message)
                .AsTask();
        }

        return Task.CompletedTask;
    }

    private async Task ProcessIncomingMessagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (await _incomingMessages
                       .Reader
                       .WaitToReadAsync(cancellationToken)
                       .ConfigureAwait(false))
            {
                while (_incomingMessages.Reader.TryRead(out var message))
                {
                    try
                    {
                        await HandleIncomingGameMessageAsync(message)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message for {AgentName}", _name);
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
                _logger.LogDebug(
                    "Agent {AgentName} is on cooldown until {NextAllowedActionTime} (current: {CurrentTime})",
                    _name,
                    _nextAllowedActionTime,
                    now);
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
        _logger.LogInformation("Processing incoming game message for {AgentName}", _name);
        _history.AddUserMessage(message);
        TrimHistory();

        // Only sometimes actually act, based on cooldown
        if (!await ShouldActAsync())
        {
            _logger.LogDebug("Cooldown active; skipping action for {AgentName}", _name);
            return;
        }

        var kernel = await GetResponse();
        var commandText = kernel.Content?.Trim();

        if (string.IsNullOrEmpty(commandText))
        {
            _logger.LogWarning("No command returned for {AgentName}");
            return;
        }

        _history.AddAssistantMessage(commandText);
        TrimHistory();

        var id = new ConnectionId(_connection.Id);

        var command = new WorldCommand
        {
            Command = commandText
        };

        _logger.LogInformation("Sending command for {AgentName}: {Command}", _name, commandText);
        await _gameInputWriter.WriteAsync(new(id, command));
    }

    private async Task<ChatMessageContent> GetResponse()
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

        var o = _options.Value;

        _logger.LogDebug("Requesting response for {AgentName} using {Source}", _name, _source);

        return _source switch
        {
            AgentSource.OpenAI => await GetOpenAIResponseAsync(o.OpenAIModelId, o.OpenAIApiKey),
            AgentSource.OpenRouter => await GetOpenAIResponseAsync(o.OpenRouterModelId,
                o.OpenRouterApiKey,
                o.OpenRouterEndpoint),
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

    private async Task<ChatMessageContent> GetOpenAIResponseAsync(string modelId,
        string apiKey,
        string? endpoint = null)
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

        return await chat.GetChatMessageContentAsync(_history,
            executionSettings: new GeminiPromptExecutionSettings
            {
                MaxTokens = 500
            },
            kernel: kernel);
    }

    private async Task<ChatMessageContent> GetAnthropicResponseAsync(AgentOptions options)
    {
        using var client = new AnthropicClient(new APIAuthentication(apiKey: options.AnthropicApiKey));
        var chatClient = (IChatClient) client.Messages;

        var messages = _history
            .Select(ConvertToChatMessage)
            .ToList();

        var response = await chatClient.GetResponseAsync(messages,
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
            var role when role == AuthorRole.Assistant => new ChatMessage(ChatRole.Assistant,
                message.Content ?? string.Empty),
            var role when role == AuthorRole.System =>
                new ChatMessage(ChatRole.System, message.Content ?? string.Empty),
            _ => new ChatMessage(ChatRole.User, message.Content ?? string.Empty)
        };
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing agent brain for {AgentName}", _name);
        await _cts.CancelAsync();
        _incomingMessages.Writer.TryComplete();

        try
        {
            if (_processingTask is not null)
            {
                await _processingTask.ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested)
        {
            // Expected during disposal
        }

        _cts.Dispose();
    }
}