using MooSharp.Actors.Players;
using MooSharp.Actors.Rooms;

namespace MooSharp.Scripting;

public interface IVerbScriptResolver
{
    VerbResolutionResult Resolve(Player player, Room room, string verb, string targetText);
}
