using MooSharp.Actors.Players;

namespace MooSharp.Features.Editor;

public interface IEditorModeHandler
{
    Task HandleEditorInputAsync(Player player, string input, CancellationToken ct = default);
}
