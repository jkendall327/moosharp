using MooSharp.Actors.Objects;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Scripting;

public record VerbResolutionResult(
    bool Found,
    Object? TargetObject = null,
    VerbScript? Script = null,
    string? ErrorMessage = null)
{
    public static VerbResolutionResult NotFound() => new(false);

    public static VerbResolutionResult Success(Object target, VerbScript script) =>
        new(true, target, script);

    public static VerbResolutionResult Error(string message) =>
        new(false, ErrorMessage: message);
}
