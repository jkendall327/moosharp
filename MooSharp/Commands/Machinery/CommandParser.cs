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
            return Task.FromResult(ParseResult.NotFound());
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
}