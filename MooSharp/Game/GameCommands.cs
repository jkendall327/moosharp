using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Game;

/// <summary>
/// Represents a command given to the game engine.
/// </summary>
public abstract record GameCommand
{
    /// <summary>
    /// A task that will be completed when the engine finishes processing this input.
    /// </summary>
    public TaskCompletionSource? CompletionSource { get; init; }
}

public record InputCommand(Guid ActorId, string Command) : GameCommand;

public record SpawnTreasureCommand(Object Treasure) : GameCommand;

public record IncrementWorldClockCommand : GameCommand;