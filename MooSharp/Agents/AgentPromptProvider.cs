using HandlebarsDotNet;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using MooSharp.Commands.Machinery;

namespace MooSharp.Agents;

public class AgentPromptProvider(IOptions<AgentOptions> options, CommandReference commandReference) : IAgentPromptProvider
{
    private readonly SemaphoreSlim _templateLock = new(1, 1);
    private HandlebarsTemplate<object, object>? _compiledTemplate;

    public async Task<string> GetSystemPromptAsync(string name,
        string persona,
        CancellationToken cancellationToken = default)
    {
        await EnsureTemplateAsync(cancellationToken).ConfigureAwait(false);

        return _compiledTemplate!(new
        {
            Name = name,
            Persona = persona,
            AvailableCommands = commandReference.BuildHelpText()
        });
    }

    public Task<ChatHistory> PrepareHistoryAsync(ChatHistory history, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(history);
    }

    private async Task EnsureTemplateAsync(CancellationToken cancellationToken)
    {
        if (_compiledTemplate is not null)
        {
            return;
        }

        await _templateLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_compiledTemplate is not null)
            {
                return;
            }

            var templatePath = Path.Combine(AppContext.BaseDirectory, options.Value.SystemPromptTemplatePath);
            templatePath = Path.GetFullPath(templatePath);
            var template = await File.ReadAllTextAsync(templatePath, cancellationToken)
                .ConfigureAwait(false);

            _compiledTemplate = Handlebars.Compile(template);
        }
        finally
        {
            _templateLock.Release();
        }
    }
}
