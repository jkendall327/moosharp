using MooSharp.Actors.Players;
using MooSharp.Actors.Rooms;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Scripting;

public record ScriptExecutionContext(
    string LuaCode,
    Object TargetObject,
    Player Actor,
    Room Location,
    string VerbName,
    string[] Arguments);
