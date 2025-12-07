using System.Diagnostics.CodeAnalysis;
using MooSharp.Commands.Machinery;

namespace MooSharp.Commands.Parsing;

public enum ParseStatus
{
    /// <summary>
    /// The command was parsed and bound successfully.
    /// </summary>
    Success,

    /// <summary>
    /// The verb was recognized, but the arguments were invalid 
    /// (e.g., target not found, missing arguments).
    /// </summary>
    Error,

    /// <summary>
    /// The verb was not recognized at all.
    /// </summary>
    NotFound
}

public class ParseResult
{
    public ParseStatus Status { get; }
    public ICommand? Command { get; }
    public string? ErrorMessage { get; }

    [MemberNotNullWhen(true, nameof(Command))]
    public bool IsSuccess => Status == ParseStatus.Success;

    private ParseResult(ParseStatus status, ICommand? command, string? errorMessage)
    {
        Status = status;
        Command = command;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// A valid command was created.
    /// </summary>
    public static ParseResult Success(ICommand command) => new(ParseStatus.Success, command, null);

    /// <summary>
    /// The definition tried to parse the command, but the input was invalid
    /// (e.g., "Target not found", "Specify an item").
    /// </summary>
    public static ParseResult Error(string userFacingMessage) => new(ParseStatus.Error, null, userFacingMessage);

    /// <summary>
    /// The input string matched no known command verbs.
    /// </summary>
    public static ParseResult NotFound() => new(ParseStatus.NotFound, null, "I don't understand that command.");
}