using System.Threading.Channels;
using MooSharp.Actors;
using MooSharp.Data;
using MooSharp.Data.Mapping;
using MooSharp.Messaging;

namespace MooSharp.Game;

public interface IGameEngine
{
    /// <summary>
    /// The main entry point for gameplay. 
    /// Called by the SignalR Hub or Agent Loop when text arrives.
    /// </summary>
    Task ProcessInputAsync(Guid actorId, string commandText, CancellationToken ct = default);

    /// <summary>
    /// Called by the Gateway when a session starts for an actor not currently in memory.
    /// Loads the player from the DB, places them in a Room, adds to World loop.
    /// </summary>
    Task SpawnActorAsync(Guid actorId, CancellationToken ct = default);

    /// <summary>
    /// Called by the Gateway when the Linkdead timer expires (or /quit is typed).
    /// Saves the player to DB, removes from Room, removes from World loop.
    /// </summary>
    Task DespawnActorAsync(Guid actorId, CancellationToken ct = default);

    /// <summary>
    /// Returns true if the actor is currently spawned in the world simulation.
    /// Used by the Gateway to decide whether to call SpawnActorAsync on reconnect.
    /// </summary>
    bool IsActorSpawned(Guid actorId);

    Task<AutocompleteOptions> GetAutocompleteOptions(Guid actorId, CancellationToken ct = default);
}

public class GameEngine(World.World world, IPlayerRepository playerRepository, ChannelWriter<NewGameInput> writer) : IGameEngine
{
    public async Task ProcessInputAsync(Guid actorId, string commandText, CancellationToken ct = default)
    {
        await writer.WriteAsync(new(actorId, commandText), ct);
    }

    public async Task SpawnActorAsync(Guid actorId, CancellationToken ct = default)
    {
        var defaultRoom = world.GetDefaultRoom();

        var player = new Player
        {
            Username = null,
            Connection = null
        };

        world.MovePlayer(player, defaultRoom);

        world.Players[null] = player;

        throw new NotImplementedException();
        
        // await hydrator.RehydrateAsync(player, dto);

        // var messages = await messageProvider.GetMessagesForLogin(player);
        //
        // await sender.SendLoginResultAsync(connectionId, true, $"Registered and logged in as {player.Username}.");
        // _ = sender.SendGameMessagesAsync(messages);    
    }

    public async Task DespawnActorAsync(Guid actorId, CancellationToken ct = default)
    {
        if (!world.Players.TryGetValue(actorId.ToString(), out var player))
        {
            return ;
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

    public async Task<AutocompleteOptions> GetAutocompleteOptions(Guid actorId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}