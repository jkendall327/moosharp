using System.Threading.Channels;
using MooSharp.Actors;
using MooSharp.Data;
using MooSharp.Data.Mapping;
using MooSharp.Infrastructure;
using MooSharp.Messaging;

namespace MooSharp.Game;

public class GameEngine(
    World.World world,
    PlayerHydrator hydrator,
    IPlayerRepository playerRepository,
    PlayerMessageProvider messageProvider,
    //IGameMessageEmitter emitter,
    ChannelWriter<GameInput> writer) : IGameEngine
{
    public async Task ProcessInputAsync(Guid actorId, string commandText, CancellationToken ct = default)
    {
        await writer.WriteAsync(new(actorId, commandText), ct);
    }

    public async Task SpawnActorAsync(Guid actorId, CancellationToken ct = default)
    {
        var defaultRoom = world.GetDefaultRoom();

        var dto = await playerRepository.LoadPlayerAsync(actorId, ct);

        if (dto is null)
        {
            throw new InvalidOperationException($"Details for actor {actorId} were not found in database.");
        }
        
        var player = await hydrator.RehydrateAsync(dto);

        world.MovePlayer(player, defaultRoom);

        world.Players[player.Id.Value.ToString()] = player;

        var messages = await messageProvider.GetMessagesForLogin(player, ct);
        
        // TODO: figure out how ot break circular dependency here.
        
       // _ = emitter.SendGameMessagesAsync(messages, ct);    
    }

    public async Task DespawnActorAsync(Guid actorId, CancellationToken ct = default)
    {
        if (!world.Players.TryGetValue(actorId.ToString(), out var player))
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
    }

    public bool IsActorSpawned(Guid actorId)
    {
        return world.Players.TryGetValue(actorId.ToString(), out var _);
    }

    public Task<AutocompleteOptions> GetAutocompleteOptions(Guid actorId, CancellationToken ct = default)
    {
        if (!world.Players.TryGetValue(actorId.ToString(), out var player))
        {
            return Task.FromResult(new AutocompleteOptions([], []));
        }

        var room = world.GetPlayerLocation(player);

        var exits = room?.Exits.Keys ?? Enumerable.Empty<string>();
        var inventory = player.Inventory.Select(item => item.Name);

        var options = new AutocompleteOptions(exits.ToList(), inventory.ToList());

        return Task.FromResult(options);
    }
}