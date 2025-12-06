using MooSharp.Actors;
using MooSharp.Agents;

namespace MooSharp.Messaging;

public record GameInput(Guid ActorId, string Command)
{
    // This task completes when the engine finishes processing this input
    public TaskCompletionSource? CompletionSource { get; init; }
}