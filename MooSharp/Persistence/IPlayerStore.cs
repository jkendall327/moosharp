namespace MooSharp.Persistence;

public interface IPlayerStore
{
    Task SaveNewPlayer(Player player, Room currentLocation, string password);
    Task SavePlayer(Player player, Room currentLocation);
    Task<PlayerDto?> LoadPlayer(LoginCommand command);
}
