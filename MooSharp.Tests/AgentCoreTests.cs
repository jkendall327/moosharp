using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using MooSharp.Agents;
using MooSharp.Messaging;
using NSubstitute;

namespace MooSharp.Tests;

public class AgentCoreTests
{
    [Fact]
    public async Task ProcessMessage_EmitsThinking_ThenResponse()
    {
        // Arrange
        var bundle = new AgentCreationBundle("Fake", "", AgentSource.OpenAI, TimeSpan.Zero, TimeSpan.Zero);

        var assistantResponse = "Hello back!";

        var provider = Substitute.For<IAgentResponseProvider>();

        provider
            .GetResponse(default!, default!, default!)
            .ReturnsForAnyArgs(new ChatMessageContent(AuthorRole.Assistant, assistantResponse));

        var core = new AgentCore(bundle,
            Substitute.For<IAgentPromptProvider>(),
            provider,
            TimeProvider.System,
            Options.Create(new AgentOptions
            {
                AnthropicApiKey = string.Empty,
                AnthropicModelId = string.Empty,
                GeminiApiKey = string.Empty,
                GeminiModelId = string.Empty,
                OpenAIApiKey = string.Empty,
                OpenAIModelId = string.Empty,
                OpenRouterApiKey = string.Empty,
                OpenRouterModelId = string.Empty,
                VolitionPrompt = string.Empty,
                AgentIdentitiesPath = string.Empty,
                SystemPromptTemplatePath = string.Empty
            }),
            NullLogger.Instance);

        await core.InitializeAsync(CancellationToken.None);

        // Act
        // We get the enumerator manually to control the stepping
        var enumerator = core
            .ProcessMessageAsync("Hello", CancellationToken.None)
            .GetAsyncEnumerator();

        // MoveNext triggers logic up to the first 'yield return'
        var hasFirst = await enumerator.MoveNextAsync();

        // We got the thinking command immediately
        Assert.True(hasFirst);
        Assert.IsType<AgentThinkingCommand>(enumerator.Current);

        // MoveNext triggers the LLM call and waits for the next yield
        var hasSecond = await enumerator.MoveNextAsync();

        // We got the text response
        Assert.True(hasSecond);
        var worldCmd = Assert.IsType<WorldCommand>(enumerator.Current);
        Assert.Equal(assistantResponse, worldCmd.Command);

        // Ensure no more commands
        var hasMore = await enumerator.MoveNextAsync();
        Assert.False(hasMore);
    }
}