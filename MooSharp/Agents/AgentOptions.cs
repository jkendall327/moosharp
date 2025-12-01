namespace MooSharp.Agents;

public class AgentOptions
{
    public const string SectionName = "Agents";

    public bool Enabled { get; set; }
    /// <summary>
    /// The number of recent non-system chat messages to retain in memory. The system prompt is always preserved.
    /// </summary>
    public int MaxRecentMessages { get; init; } = 20;
    public required string OpenAIModelId { get; init; }
    public required string OpenAIApiKey { get; init; }
    public required string GeminiModelId { get; init; }
    public required string GeminiApiKey { get; init; }
    public required string OpenRouterModelId { get; init; }
    public required string OpenRouterApiKey { get; init; }
    public string OpenRouterEndpoint { get; init; } = "https://openrouter.ai/api/v1";
    public required string AnthropicModelId { get; init; }
    public required string AnthropicApiKey { get; init; }
    public required string AgentIdentitiesPath { get; init; }
    public required string SystemPromptTemplatePath { get; init; }
    public required string VolitionPrompt { get; init; }
    public TimeSpan DefaultActionCooldown { get; init; } = TimeSpan.FromSeconds(5);
}
