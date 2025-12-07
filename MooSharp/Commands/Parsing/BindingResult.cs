namespace MooSharp.Commands.Parsing;

public record BindingResult<T>(T? Value, string? ErrorMessage, bool IsSuccess)
{
    public static BindingResult<T> Success(T value) => new(value, null, true);
    public static BindingResult<T> Failure(string error) => new(default, error, false);
}