using Microsoft.Extensions.Logging;
using MooSharp.Actors;

namespace MooSharp.Commands.Machinery;

/// <summary>
/// Parses raw text into commands.
/// </summary>
public class CommandParser
{
    private readonly ILogger<CommandParser> _logger;
    private readonly Dictionary<string, ICommandDefinition> _verbs;
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
        var trimmed = command.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return NullCommand;
        }

        var split = trimmed
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var sanitized = string.Join(' ', split);

        _logger.LogDebug("Parsing player input: {Input}", sanitized);

        if (TryParseShortcut(player, trimmed, out var shortcut))
        {
            return Task.FromResult(shortcut);
        }

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

    private bool TryParseShortcut(Player player, string input, out ICommand? command)
    {
        var prefix = input[0];

        if (prefix is '"' or '\'' && _verbs.TryGetValue("say", out var sayDefinition))
        {
            var args = NormalizeShortcutArgs(input[1..]);

            command = sayDefinition.Create(player, args);
            return true;
        }

        if (prefix == ':' && _verbs.TryGetValue("/me", out var emoteDefinition))
        {
            var args = NormalizeShortcutArgs(input[1..]);

            command = emoteDefinition.Create(player, args);
            return true;
        }

        command = null;
        return false;
    }

    private static string NormalizeShortcutArgs(string shortcutBody)
        => string.Join(' ', shortcutBody.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
