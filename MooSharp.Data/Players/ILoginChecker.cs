namespace MooSharp.Data;

public interface ILoginChecker
{
    Task<LoginResult> LoginIsValidAsync(string username, string password, CancellationToken ct = default);
}

public enum LoginResult
{
    Ok,
    UsernameNotFound,
    WrongPassword
}