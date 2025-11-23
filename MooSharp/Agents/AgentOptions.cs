namespace MooSharp.Agents;

public class AgentOptions
{
    public const string SectionName = "Agents";
    
    public bool Enabled { get; set; }
    public required string OpenAIModelId { get; set; }
    public required string OpenAIApiKey { get; set; }
    public required string GeminiModelId { get; set; }
    public required string GeminiApiKey { get; set; }
}