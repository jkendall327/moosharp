using System.Text;

namespace MooSharp;

public class CommandReference
{
    private readonly IReadOnlyCollection<ICommandDefinition> _definitions;

    public CommandReference(IEnumerable<ICommandDefinition> definitions)
    {
        _definitions = definitions.ToArray();
    }

    public string BuildHelpText()
    {
        var sb = new StringBuilder();

        sb.AppendLine("Available commands:");

        var categories = _definitions
            .GroupBy(s => s.Category)
            .ToList();

        foreach (var category in categories)
        {
            sb.AppendLine($"===[{category.Key.ToString()}]===");
            sb.AppendLine();

            var definitions = category
                .OrderBy(s => s.Verbs.First())
                .ToList();

            foreach (var definition in definitions)
            {
                var primaryVerb = definition.Verbs.First();

                var synonyms = definition
                    .Verbs
                    .Skip(1)
                    .ToList();

                sb.Append($"- [{primaryVerb}]: {definition.Description}");

                if (!definition.Description.EndsWith('.'))
                {
                    sb.Append('.');
                }
                
                if (synonyms.Any())
                {
                    var verbs = string.Join(", ", synonyms);
                    sb.Append($" Synonyms: [{verbs}]");
                }

                sb.AppendLine();
            }

            sb.AppendLine();
        }

        return sb
            .ToString()
            .TrimEnd();
    }
}