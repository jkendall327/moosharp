using MooSharp.Commands.Presentation;

namespace MooSharp.Commands.Commands.Scripting;

/// <summary>
/// Event for output from a script (messages sent to players).
/// </summary>
public record ScriptOutputEvent(string Message) : IGameEvent;

public class ScriptOutputEventFormatter : IGameEventFormatter<ScriptOutputEvent>
{
    public string FormatForActor(ScriptOutputEvent gameEvent) => gameEvent.Message;

    public string? FormatForObserver(ScriptOutputEvent gameEvent) => gameEvent.Message;
}

/// <summary>
/// Event for script execution errors.
/// </summary>
public record ScriptErrorEvent(string ErrorMessage) : IGameEvent;

public class ScriptErrorEventFormatter : IGameEventFormatter<ScriptErrorEvent>
{
    public string FormatForActor(ScriptErrorEvent gameEvent) => $"[Script Error] {gameEvent.ErrorMessage}";

    public string? FormatForObserver(ScriptErrorEvent gameEvent) => null;
}
