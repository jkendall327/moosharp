using System.Text;

namespace MooSharp.Commands.Machinery;

public class CommandReference(IEnumerable<ICommandDefinition> definitions)
{
    private readonly IReadOnlyCollection<ICommandDefinition> _definitions = definitions.ToArray();

    public IReadOnlyCollection<CommandCategoryEntry> GetCommandMetadata()
    {
        return _definitions
            .GroupBy(definition => definition.Category)
            .OrderBy(group => (int)group.Key)
            .Select(group => new CommandCategoryEntry(
                group.Key,
                group
                    .OrderBy(definition => definition.Verbs.First())
                    .Select(definition => new CommandReferenceEntry(
                        definition.Verbs.First(),
                        definition.Verbs.Skip(1).ToArray(),
                        definition.Description,
                        definition.Category))
                    .ToArray()))
            .ToArray();
    }

    public string BuildHelpText()
    {
        var sb = new StringBuilder();

        sb.AppendLine("Available commands:");

        var categories = GetCommandMetadata();

        foreach (var category in categories)
        {
            sb.AppendLine($"===[{category.Category.ToString()}]===");
            sb.AppendLine();

            foreach (var definition in category.Commands)
            {
                var synonyms = definition.Synonyms;

                sb.Append($"- [{definition.PrimaryVerb}]: {definition.Description}");

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

    public sealed record CommandCategoryEntry(CommandCategory Category, IReadOnlyCollection<CommandReferenceEntry> Commands);

    public sealed record CommandReferenceEntry(
        string PrimaryVerb,
        IReadOnlyCollection<string> Synonyms,
        string Description,
        CommandCategory Category);
}