namespace MooSharp.Data.Players;

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