using MooSharp.Agents;
using MooSharp.Messaging;

namespace MooSharp;

public record GameInput(ConnectionId ConnectionId, InputCommand Command, string? SessionToken = null)
{
    // This task completes when the engine finishes processing this input
    public TaskCompletionSource? CompletionSource { get; init; }
}

public abstract class InputCommand;

public class RegisterCommand : InputCommand
{
    public required string Username { get; init; }
    public required string Password { get; init; }
}

public class LoginCommand : InputCommand
{
    public required string Username { get; init; }
    public required string Password { get; init; }
}

public class RegisterAgentCommand : InputCommand
{
    public required AgentIdentity Identity { get; init; }
    public required IPlayerConnection Connection { get; init; }
}

public class WorldCommand : InputCommand
{
    public required string Command { get; init; }
}

public class DisconnectCommand : InputCommand;

public class ReconnectCommand : InputCommand;

public class AgentThinkingCommand : InputCommand;
