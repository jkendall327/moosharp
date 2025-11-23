namespace MooSharp.Agents;

public class AgentOptions
{
    public const string SectionName = "Agents";

    public bool Enabled { get; set; }
    public required string OpenAIModelId { get; set; }
    public required string OpenAIApiKey { get; set; }
    public required string GeminiModelId { get; set; }
    public required string GeminiApiKey { get; set; }
    public required string OpenRouterModelId { get; set; }
    public required string OpenRouterApiKey { get; set; }
    public string OpenRouterEndpoint { get; set; } = "https://openrouter.ai/api/v1";
    public required string AnthropicModelId { get; set; }
    public required string AnthropicApiKey { get; set; }
    public required string AgentIdentitiesPath { get; set; }
}