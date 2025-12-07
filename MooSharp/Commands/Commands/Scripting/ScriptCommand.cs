using MooSharp.Actors.Objects;
using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Commands.Commands.Scripting;

public class ScriptCommand : CommandBase<ScriptCommand>
{
    public required Player Player { get; init; }
    public required Object TargetObject { get; init; }
    public required VerbScript Script { get; init; }
    public required string VerbName { get; init; }
    public string[] Arguments { get; init; } = [];
}
