using MooSharp.Actors;
using MooSharp.Messaging;
using MooSharp.Persistence.Dtos;

namespace MooSharp.Persistence;

public interface IPlayerStore
{
    Task SaveNewPlayer(Player player, Room currentLocation, string password, CancellationToken ct = default);
    Task SavePlayer(Player player, Room currentLocation, CancellationToken ct = default);
    Task<PlayerDto?> LoadPlayer(LoginCommand command, CancellationToken ct = default);
}
