using Microsoft.SemanticKernel.ChatCompletion;

namespace MooSharp.Agents;

public interface IAgentPromptProvider
{
    Task<string> GetSystemPromptAsync(string name,
        string persona,
        CancellationToken cancellationToken = default);

    Task<ChatHistory> PrepareHistoryAsync(ChatHistory history, CancellationToken cancellationToken = default);
}
