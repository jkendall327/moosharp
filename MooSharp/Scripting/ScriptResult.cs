using MooSharp.Actors.Players;

namespace MooSharp.Scripting;

public record ScriptResult(
    bool Success,
    string? ErrorMessage = null,
    IReadOnlyList<ScriptMessage>? Messages = null)
{
    public static ScriptResult Ok(IReadOnlyList<ScriptMessage>? messages = null) =>
        new(true, null, messages);

    public static ScriptResult Error(string message) =>
        new(false, message);
}

public record ScriptMessage(
    Player Recipient,
    string Text);
