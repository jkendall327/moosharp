using Microsoft.Extensions.Logging;
using MooSharp.Actors.Players;
using MooSharp.Commands.Parsing;

namespace MooSharp.Commands.Machinery;

/// <summary>
/// Parses raw text into commands.
/// </summary>
public class CommandParser
{
    private readonly World.World _world;
    private readonly ArgumentBinder _binder;
    private readonly ILogger<CommandParser> _logger;
    
    private readonly Dictionary<string, ICommandDefinition> _verbs;

    public CommandParser(ILogger<CommandParser> logger,
        IEnumerable<ICommandDefinition> definitions,
        World.World world,
        ArgumentBinder binder)
    {
        _logger = logger;
        _world = world;
        _binder = binder;

        // Build verb to definition map once.
        _verbs = definitions
            .SelectMany(def => def.Verbs.Select(v => (verb: v, def)))
            .ToDictionary(x => x.verb, x => x.def, StringComparer.OrdinalIgnoreCase);
    }

    public Task<ParseResult> ParseAsync(Player player, string input, CancellationToken ct = default)
    {
        var tokens = StringTokenizer.Tokenize(input);

        if (tokens.Count == 0)
        {
            return Task.FromResult(ParseResult.NotFound());
        }

        var verb = tokens.Dequeue();

        if (!_verbs.TryGetValue(verb, out var def))
        {
            return Task.FromResult(ParseResult.Error("I don't understand that command."));
        }

        var room = _world.GetLocationOrThrow(player);
        var context = new ParsingContext(player, room, tokens);

        var error = def.TryCreateCommand(context, _binder, out var command);

        if (error != null)
        {
            return Task.FromResult(ParseResult.Error(error));
        }

        return Task.FromResult(ParseResult.Success(command!));
    }

    // public Task<ICommand?> ParseAsync(Player player, string command, CancellationToken token = default)
    // {
    //     var trimmed = command.Trim();
    //
    //     if (string.IsNullOrWhiteSpace(trimmed))
    //     {
    //         return NullCommand;
    //     }
    //
    //     var split = trimmed
    //         .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    //
    //     var sanitized = string.Join(' ', split);
    //
    //     _logger.LogDebug("Parsing player input: {Input}", sanitized);
    //
    //     if (TryParseShortcut(player, trimmed, out var shortcut))
    //     {
    //         return Task.FromResult(shortcut);
    //     }
    //
    //     var verb = split.FirstOrDefault();
    //
    //     if (string.IsNullOrWhiteSpace(verb))
    //     {
    //         return NullCommand;
    //     }
    //
    //     var args = string.Join(' ', split.Skip(1));
    //
    //     if (!_verbs.TryGetValue(verb, out var definition))
    //     {
    //         return NullCommand;
    //     }
    //
    //     var cmd = definition.Create(player, args);
    //
    //     return Task.FromResult<ICommand?>(cmd);
    // }

    // private bool TryParseShortcut(Player player, string input, out ICommand? command)
    // {
    //     var prefix = input[0];
    //
    //     if (prefix is '"' or '\'' && _verbs.TryGetValue("say", out var sayDefinition))
    //     {
    //         var args = NormalizeShortcutArgs(input[1..]);
    //
    //         command = sayDefinition.Create(player, args);
    //         return true;
    //     }
    //
    //     if (prefix == ':' && _verbs.TryGetValue("/me", out var emoteDefinition))
    //     {
    //         var args = NormalizeShortcutArgs(input[1..]);
    //
    //         command = emoteDefinition.Create(player, args);
    //         return true;
    //     }
    //
    //     command = null;
    //     return false;
    // }

    private static string NormalizeShortcutArgs(string shortcutBody) =>
        string.Join(' ',
            shortcutBody.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}