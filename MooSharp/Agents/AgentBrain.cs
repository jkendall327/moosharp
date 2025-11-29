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
using System.Diagnostics;
using System.Threading.Channels;

namespace MooSharp.Agents;

public sealed class AgentBrain : IAsyncDisposable
{
    // State
    private readonly AgentPlayerConnection _connection;
    private readonly ChatHistory _history;
    private readonly AgentCreationBundle _bundle;
    
    // Services
    private readonly IAgentPromptProvider _promptProvider;
    private readonly TimeProvider _clock;
    private readonly IAgentResponseProvider _responseProvider;
    private readonly IOptions<AgentOptions> _options;
    private readonly ILogger _logger;

    // Mailbox loop processing
    private readonly ChannelWriter<GameInput> _gameInputWriter;
    private readonly Channel<string> _incomingMessages;
    private readonly CancellationTokenSource _cts;
    private Task? _processingTask;
    
    // Volition
    private Task? _volitionTask;

    // Rate limiting
    private DateTimeOffset _nextAllowedActionTime = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _cooldownLock = new(1, 1);

    public IPlayerConnection Connection => _connection;
    
    public AgentBrain(AgentCreationBundle bundle,
        ChannelWriter<GameInput> gameInputWriter,
        IAgentPromptProvider promptProvider,
        TimeProvider clock,
        IAgentResponseProvider responseProvider,
        IOptions<AgentOptions> options,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        _bundle = bundle;
        _gameInputWriter = gameInputWriter;
        _logger = logger;
        _responseProvider = responseProvider;
        _options = options;
        _promptProvider = promptProvider;
        _clock = clock;

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
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

        _history = [];
    }
    
    public async Task StartAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting agent brain for {AgentName} (source: {AgentSource})", _bundle.Name, _bundle.Source);

        await EnsureSystemPromptAsync(ct)
            .ConfigureAwait(false);

        _processingTask = Task.Run(() => ProcessIncomingMessagesAsync(_cts.Token), ct);
        _volitionTask = Task.Run(() => HandleVolitionAsync(_cts.Token), ct);
    }
    
    private async Task HandleVolitionAsync(CancellationToken ct = default)
    {
        using var timer = new PeriodicTimer(_bundle.VolitionCooldown, _clock);

        while (await timer.WaitForNextTickAsync(ct))
        {
            await EnqueueIncomingMessageAsync(_options.Value.VolitionPrompt);
        }
    }

    private async Task EnsureSystemPromptAsync(CancellationToken cancellationToken)
    {
        if (_history.Count > 0)
        {
            return;
        }

        var systemPrompt = await _promptProvider
            .GetSystemPromptAsync(_bundle.Name, _bundle.Persona, cancellationToken)
            .ConfigureAwait(false);

        _history.AddSystemMessage(systemPrompt);
    }

    private async Task EnqueueIncomingMessageAsync(string message)
    {
        _logger.LogDebug("Queuing incoming message for {AgentName}: {Message}", _bundle.Name, message);

        await _incomingMessages.Writer.WriteAsync(message).ConfigureAwait(false);
    }

    private async Task ProcessIncomingMessagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var message in _incomingMessages.Reader.ReadAllAsync(cancellationToken))
            {
                await HandleIncomingGameMessageAsync(message).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during disposal
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message for {AgentName}", _bundle.Name);
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
                    _bundle.Name,
                    _nextAllowedActionTime,
                    now);

                return false;
            }

            // We’re allowed to act now; set the next allowed time
            _nextAllowedActionTime = now + _bundle.ActionCooldown;

            return true;
        }
        finally
        {
            _cooldownLock.Release();
        }
    }

    private async Task PublishThinkingAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _gameInputWriter
                .WriteAsync(new(new(_connection.Id), new AgentThinkingCommand()), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutdown in progress; no-op
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish thinking indicator for {AgentName}", _bundle.Name);
        }
    }

    private async Task HandleIncomingGameMessageAsync(string message)
    {
        // Always record history.
        _logger.LogInformation("Processing incoming game message for {AgentName}", _bundle.Name);
        _history.AddUserMessage(message);

        TrimHistory();

        // Only sometimes actually act, based on cooldown
        if (!await ShouldActAsync())
        {
            _logger.LogDebug("Cooldown active; skipping action for {AgentName}", _bundle.Name);

            return;
        }

        await PublishThinkingAsync(_cts.Token)
            .ConfigureAwait(false);

        var content = await _responseProvider
            .GetResponse(_bundle.Name, _bundle.Source, _history)
            .ConfigureAwait(false);

        var commandText = content.Content?.Trim();

        if (string.IsNullOrEmpty(commandText))
        {
            _logger.LogWarning("No command returned for {AgentName}", _bundle.Name);

            return;
        }

        _history.AddAssistantMessage(commandText);

        TrimHistory();

        var id = new ConnectionId(_connection.Id);

        var command = new WorldCommand
        {
            Command = commandText
        };

        _logger.LogInformation("Sending command for {AgentName}: {Command}", _bundle.Name, commandText);
        await _gameInputWriter.WriteAsync(new(id, command));
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

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing agent brain for {AgentName}", _bundle.Name);
        await _cts.CancelAsync();
        _incomingMessages.Writer.TryComplete();

        try
        {
            if (_processingTask is not null)
            {
                await _processingTask.ConfigureAwait(false);
            }
            if (_volitionTask is not null)
            {
                await _volitionTask.ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested)
        {
            // Expected during disposal
        }

        _cts.Dispose();
    }
}