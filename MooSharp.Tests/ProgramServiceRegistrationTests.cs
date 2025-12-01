using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MooSharp.Web;

namespace MooSharp.Tests;

public class ProgramServiceRegistrationTests
{
    [Fact]
    public void Program_ServiceRegistrations_BuildSuccessfully()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });

        builder.Configuration.AddInMemoryCollection(CreateRequiredConfiguration());

        builder.Services.AddSignalR();
        builder.Services.AddMooSharpServices(builder.Configuration);
        builder.Services.AddMooSharpOptions(builder.Configuration);
        builder.Services.AddMooSharpHostedServices(builder.Configuration);
        builder.Services.AddMooSharpMessaging(builder.Configuration);

        builder.RegisterCommandDefinitions();
        builder.RegisterCommandHandlers();
        builder.RegisterPresenters();

        using var serviceProvider = builder.Services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        using var scope = serviceProvider.CreateScope();

        Assert.NotNull(scope.ServiceProvider);
    }

    private static Dictionary<string, string?> CreateRequiredConfiguration() => new()
    {
        ["AppOptions:WorldDataFilepath"] = "world.json",
        ["AppOptions:DatabaseFilepath"] = "game.db",
        ["Agents:Enabled"] = "false",
        ["Agents:MaxRecentMessages"] = "10",
        ["Agents:OpenAIModelId"] = "test-openai-model",
        ["Agents:OpenAIApiKey"] = "test-openai-key",
        ["Agents:GeminiModelId"] = "test-gemini-model",
        ["Agents:GeminiApiKey"] = "test-gemini-key",
        ["Agents:OpenRouterModelId"] = "test-openrouter-model",
        ["Agents:OpenRouterApiKey"] = "test-openrouter-key",
        ["Agents:OpenRouterEndpoint"] = "https://openrouter.ai/api/v1",
        ["Agents:AnthropicModelId"] = "test-anthropic-model",
        ["Agents:AnthropicApiKey"] = "test-anthropic-key",
        ["Agents:AgentIdentitiesPath"] = "agents.json"
    };
}
