namespace MooSharp.Data;

public interface ILoginChecker
{
    Task<LoginResult> LoginIsValidAsync(string username, string password, CancellationToken ct);
}

public enum LoginResult
{
    Ok,
    UsernameNotFound,
    WrongPassword
}