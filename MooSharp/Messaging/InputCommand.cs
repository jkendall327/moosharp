namespace MooSharp.Messaging;

public abstract record GameCommand
{
    // This task completes when the engine finishes processing this input
    public TaskCompletionSource? CompletionSource { get; init; }
}

public record InputCommand(Guid ActorId, string Command) : GameCommand;

public record SpawnTreasureCommand(MooSharp.Actors.Object Treasure) : GameCommand;