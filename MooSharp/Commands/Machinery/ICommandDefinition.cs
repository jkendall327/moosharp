using MooSharp.Actors;
using MooSharp.Actors.Players;

namespace MooSharp.Commands.Machinery;

/// <summary>
/// Defines how a command is parsed from raw text.
/// </summary>
public interface ICommandDefinition
{
    /// <summary>
    /// Synonyms that can be used to invoke the command.
    /// </summary>
    IReadOnlyCollection<string> Verbs { get; }

    /// <summary>
    /// Short description of what the command does, for help text.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Parses raw text into the command.
    /// </summary>
    ICommand Create(Player player, string args);

    CommandCategory Category { get; }
}

public enum CommandCategory
{
    Admin,
    Meta,
    General,
    Social
}