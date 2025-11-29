using System.Text;

namespace MooSharp;

public class CommandReference
{
    private readonly IReadOnlyCollection<ICommandDefinition> _definitions;

    public CommandReference(IEnumerable<ICommandDefinition> definitions)
    {
        _definitions = definitions
            .ToArray();
    }

    public string BuildHelpText()
    {
        var sb = new StringBuilder();

        sb.AppendLine("Available commands:");

        foreach (var definition in _definitions.OrderBy(d => d.Verbs.First()))
        {
            var verbs = string.Join(", ", definition.Verbs);

            sb.AppendLine($"- {verbs}: {definition.Description}");
        }

        return sb.ToString().TrimEnd();
    }
}
