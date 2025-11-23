namespace MooSharp.Persistence;

public interface IPlayerStore
{
    Task SaveNewPlayer(Player player, string password);
    Task<PlayerDto?> LoadPlayer(LoginCommand command);
}

public class JsonPlayerStore : IPlayerStore
{
    private readonly List<PlayerDto> _players = [new()
    {
        Username = "Jane Doe",
        Password = "hunter123"
    }];

    public Task SaveNewPlayer(Player player, string password)
    {
        _players.Add(new()
        {
            Username = player.Username,
            Password = password,
            CurrentLocation = player.CurrentLocation.Id
        });

        return Task.CompletedTask;
    }

    public Task<PlayerDto?> LoadPlayer(LoginCommand command)
    {
        var player = _players.SingleOrDefault(s => s.Username == command.Username && s.Password == command.Password);

        return Task.FromResult(player);
    }
}