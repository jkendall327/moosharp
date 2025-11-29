using Microsoft.Extensions.Logging;

namespace MooSharp;

/// <summary>
/// Parses raw text into commands.
/// </summary>
public class CommandParser
{
    private readonly ILogger<CommandParser> _logger;
    private readonly IReadOnlyDictionary<string, ICommandDefinition> _verbs;
    private static readonly Task<ICommand?> NullCommand = Task.FromResult<ICommand?>(null);

    public CommandParser(ILogger<CommandParser> logger, IEnumerable<ICommandDefinition> definitions)
    {
        _logger = logger;

        // Build verb to definition map once.
        _verbs = definitions
            .SelectMany(def => def.Verbs.Select(v => (verb: v, def)))
            .ToDictionary(x => x.verb, x => x.def, StringComparer.OrdinalIgnoreCase);
    }

    public Task<ICommand?> ParseAsync(Player player, string command, CancellationToken token = default)
    {
        var split = command
            .Trim()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var sanitized = string.Join(' ', split);

        _logger.LogDebug("Parsing player input: {Input}", sanitized);

        var verb = split.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(verb))
        {
            return NullCommand;
        }

        var args = string.Join(' ', split.Skip(1));

        if (!_verbs.TryGetValue(verb, out var definition))
        {
            return NullCommand;
        }

        var cmd = definition.Create(player, args);

        return Task.FromResult<ICommand?>(cmd);
    }
}