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
}