using MooSharp.Commands.Presentation;

namespace MooSharp.Features.Editor;

public record EditorModeEnteredEvent(string ObjectName, string VerbName) : IGameEvent;

public class EditorModeEnteredEventFormatter : IGameEventFormatter<EditorModeEnteredEvent>
{
    public string FormatForActor(EditorModeEnteredEvent e) =>
        $"Entering editor mode for '{e.ObjectName}:{e.VerbName}'.\n" +
        "Enter Lua code line by line. Type '.' on a blank line to save and exit, or '@abort' to cancel.";

    public string? FormatForObserver(EditorModeEnteredEvent e) => null;
}

public record EditorLineEchoEvent(int LineNumber, string Line) : IGameEvent;

public class EditorLineEchoEventFormatter : IGameEventFormatter<EditorLineEchoEvent>
{
    public string FormatForActor(EditorLineEchoEvent e) => $"{e.LineNumber}: {e.Line}";

    public string? FormatForObserver(EditorLineEchoEvent e) => null;
}

public record VerbEditedEvent(string ObjectName, string VerbName, int LineCount) : IGameEvent;

public class VerbEditedEventFormatter : IGameEventFormatter<VerbEditedEvent>
{
    public string FormatForActor(VerbEditedEvent e) =>
        $"Verb '{e.ObjectName}:{e.VerbName}' saved ({e.LineCount} lines).";

    public string? FormatForObserver(VerbEditedEvent e) => null;
}

public record EditorModeCancelledEvent(string ObjectName, string VerbName) : IGameEvent;

public class EditorModeCancelledEventFormatter : IGameEventFormatter<EditorModeCancelledEvent>
{
    public string FormatForActor(EditorModeCancelledEvent e) =>
        $"Editing of '{e.ObjectName}:{e.VerbName}' cancelled. No changes saved.";

    public string? FormatForObserver(EditorModeCancelledEvent e) => null;
}

public record VerbNotFoundEvent(string ObjectName, string VerbName) : IGameEvent;

public class VerbNotFoundEventFormatter : IGameEventFormatter<VerbNotFoundEvent>
{
    public string FormatForActor(VerbNotFoundEvent e) =>
        $"'{e.ObjectName}' has no verb named '{e.VerbName}'. Use @verb {e.ObjectName}:{e.VerbName} to create it first.";

    public string? FormatForObserver(VerbNotFoundEvent e) => null;
}
