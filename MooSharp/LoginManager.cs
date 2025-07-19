namespace MooSharp;

public interface ILoginManager
{
    Task<PlayerConnection?> AttemptLoginAsync(Stream stream, CancellationToken token = default);
}

public class LoginManager
{
    public Task<PlayerConnection?> AttemptLoginAsync(Stream stream, CancellationToken token = default)
    {
        var player = new Player
        {
            Username = "janedoe"
        };
        
        var conn = new PlayerConnection(stream, player);

        return Task.FromResult<PlayerConnection?>(conn);
    }
}