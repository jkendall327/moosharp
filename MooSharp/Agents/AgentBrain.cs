using Microsoft.SemanticKernel.Connectors.OpenAI;
using MooSharp.Messaging;

namespace MooSharp.Agents;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Threading.Channels;

public class AgentBrain
{
    private readonly AgentPlayerConnection _connection;
    private readonly ChannelWriter<GameInput> _gameInputWriter;
    private readonly IChatCompletionService _chatService;
    private readonly Kernel _kernel;
    private readonly ChatHistory _history;
    private readonly string _persona;

    // Rate limiting fields
    private readonly TimeSpan _actionCooldown;
    private DateTimeOffset _nextAllowedActionTime = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _cooldownLock = new(1, 1);

    public AgentBrain(string name,
        string persona,
        ChannelWriter<GameInput> gameInputWriter,
        IChatCompletionService chatService,
        Kernel kernel,
        TimeSpan? actionCooldown = null)
    {
        _persona = persona;
        _gameInputWriter = gameInputWriter;
        _chatService = chatService;
        _kernel = kernel;

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
        var now = DateTimeOffset.UtcNow;

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

        // Only sometimes actually act, based on cooldown
        if (!await ShouldActAsync()
                .ConfigureAwait(false))
        {
            return;
        }

        // We limit tokens to prevent it from writing a novel, we just want a command.
        var result = await _chatService.GetChatMessageContentAsync(_history,
            executionSettings: new OpenAIPromptExecutionSettings
            {
                MaxTokens = 50
            },
            kernel: _kernel);

        var commandText = result.Content?.Trim();

        if (string.IsNullOrEmpty(commandText))
        {
            return;
        }

        _history.AddAssistantMessage(commandText);

        await _gameInputWriter.WriteAsync(new(new(_connection.Id),
            new WorldCommand
            {
                Command = commandText
            }));
    }
}