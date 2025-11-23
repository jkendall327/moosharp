using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using MooSharp.Messaging;

namespace MooSharp.Agents;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Threading.Channels;

public enum AgentSource
{
    OpenAI,
    Gemini,
    OpenRouter
}

public class AgentFactory(ChannelWriter<GameInput> writer, TimeProvider clock, IOptions<AgentOptions> options)
{
    public AgentBrain Build(string name, string persona, AgentSource source, TimeSpan? cooldown = null)
    {
        return new(name, persona, source, writer, options, clock, cooldown);
    }
}

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
        if (!await ShouldActAsync())
        {
            return;
        }

        var kernel = await GetResponse();
        var commandText = kernel.Content?.Trim();

        commandText = "examine me";

        if (string.IsNullOrEmpty(commandText))
        {
            return;
        }

        _history.AddAssistantMessage(commandText);

        var id = new ConnectionId(_connection.Id);

        var command = new WorldCommand
        {
            Command = commandText
        };

        await _gameInputWriter.WriteAsync(new(id, command));
    }

    private async Task<ChatMessageContent> GetResponse()
    {
        if (_source is AgentSource.OpenAI)
        {
            var kernel = Kernel
                .CreateBuilder()
                .AddOpenAIChatCompletion("", "")
                .Build();

            var chat = kernel.Services.GetRequiredService<IChatCompletionService>();

            return await chat.GetChatMessageContentAsync(_history,
                executionSettings: new OpenAIPromptExecutionSettings
                {
                    MaxTokens = 500
                },
                kernel: kernel);
        }

        throw new NotImplementedException();
    }
}