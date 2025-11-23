namespace MooSharp;

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
    /// Parses raw text into the command.
    /// </summary>
    ICommand Create(Player player, string args);
}