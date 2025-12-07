using MooSharp.Actors.Players;
using MooSharp.Actors.Rooms;
using MooSharp.Commands.Commands.Scripting;

namespace MooSharp.Scripting;

public interface IVerbScriptResolver
{
    VerbResolutionResult Resolve(Player player, Room room, string verb, string targetText);

    /// <summary>
    /// Attempts to parse the input and resolve it to a script command.
    /// </summary>
    ScriptCommand? TryResolveCommand(Player player, Room room, string input);
}
