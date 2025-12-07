using System.Diagnostics.CodeAnalysis;

namespace MooSharp.Commands.Parsing;

public record BindingResult<T>(T? Value, string? ErrorMessage, bool IsSuccess)
{
    // This property only exists because we can't apply this attribute to the constructor-style parameter above.
    [MemberNotNullWhen(true, nameof(Value))]
    public bool IsSuccess { get; init; } = IsSuccess;

    public static BindingResult<T> Success(T value) => new(value, null, true);
    public static BindingResult<T> Failure(string error) => new(default, error, false);
}