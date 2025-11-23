namespace MooSharp.Persistence;

public interface IPlayerStore
{
    Task SaveNewPlayer(Player player, string password);
    Task<PlayerDto?> LoadPlayer(LoginCommand command);
}

public class JsonPlayerStore : IPlayerStore
{
    public Task SaveNewPlayer(Player player, string password)
    {
        throw new NotImplementedException();
    }

    public Task<PlayerDto?> LoadPlayer(LoginCommand command)
    {
        throw new NotImplementedException();
    }
}