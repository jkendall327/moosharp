using System.Threading.Channels;
using MooSharp.Actors.Players;
using MooSharp.Data;
using MooSharp.Data.Players;
using MooSharp.Features.Autocomplete;
using MooSharp.Infrastructure;

namespace MooSharp.Game;

public class GameEngine(
    World.World world,
    PlayerHydrator hydrator,
    IPlayerRepository playerRepository,
    ChannelWriter<GameCommand> writer,
    MooSharpMetrics metrics) : IGameEngine
{
    public event Action<Player>? OnPlayerSpawned;
    public event Action<Player>? OnPlayerDespawned;

    public async Task ProcessInputAsync(Guid actorId, string commandText, CancellationToken ct = default)
    {
        await writer.WriteAsync(new InputCommand(actorId, commandText), ct);
    }

    public async Task SpawnActorAsync(Guid actorId, CancellationToken ct = default)
    {
        var dto = await playerRepository.LoadPlayerAsync(actorId, ct);

        if (dto is null)
        {
            throw new InvalidOperationException($"Details for actor {actorId} were not found in database.");
        }

        var player = await hydrator.RehydrateAsync(dto);

        world.RegisterPlayer(player);

        metrics.RecordPlayerOnline();
        metrics.RecordLogin();

        OnPlayerSpawned?.Invoke(player);
    }

    public async Task DespawnActorAsync(Guid actorId, CancellationToken ct = default)
    {
        var player = world.TryGetPlayer(actorId);

        if (player is null)
        {
            return;
        }

        // If they've somehow ended up in a broken state, restore them to the default location.
        var location = world.GetPlayerLocation(player);

        if (location is null)
        {
            location = world.GetDefaultRoom();

            world.MovePlayer(player, location);
        }

        var snapshot = PlayerSnapshotFactory.CreateSnapshot(player, location);

        await playerRepository.SavePlayerAsync(snapshot, WriteType.Deferred, ct);

        world.RemovePlayer(player);

        metrics.RecordPlayerOffline();
        metrics.RecordLogout();

        OnPlayerDespawned?.Invoke(player);
    }

    public bool IsActorSpawned(Guid actorId)
    {
        return world.TryGetPlayer(actorId) is not null;
    }
}