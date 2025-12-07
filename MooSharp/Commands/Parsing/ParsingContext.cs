using MooSharp.Actors.Players;
using MooSharp.Actors.Rooms;

namespace MooSharp.Commands.Parsing;

public class ParsingContext(Player player, Room room, Queue<string> tokens)
{
    public Player Player { get; } = player;
    public Room Room { get; } = room;
    public Queue<string> Tokens { get; } = tokens;

    // Helper to peek without consuming
    public string? Peek() => Tokens.Count > 0 ? Tokens.Peek() : null;

    // Helper to consume the next token
    public string? Pop() => Tokens.Count > 0 ? Tokens.Dequeue() : null;

    // Check if we have more arguments
    public bool IsFinished => Tokens.Count == 0;

    /// <summary>
    /// Consumes all remaining tokens and joins them into a single string.
    /// Used for chat messages (Say, Whisper, Emote).
    /// </summary>
    public string GetRemainingText()
    {
        if (Tokens.Count == 0)
        {
            return string.Empty;
        }

        // string.Join automatically handles the spacing between tokens.
        // Since our Tokenizer preserved the words but stripped syntax, 
        // this reconstructs a clean sentence.
        var text = string.Join(" ", Tokens);
        Tokens.Clear();
        return text;
    }

}