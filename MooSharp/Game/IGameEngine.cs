using System.Threading.Channels;
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

public class GameEngine(World.World world, ChannelWriter<NewGameInput> writer) : IGameEngine
{
    public async Task ProcessInputAsync(Guid actorId, string commandText, CancellationToken ct = default)
    {
        await writer.WriteAsync(new(actorId, commandText), ct);
    }

    public async Task SpawnActorAsync(Guid actorId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task DespawnActorAsync(Guid actorId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public bool IsActorSpawned(Guid actorId)
    {
        throw new NotImplementedException();
    }

    public async Task<AutocompleteOptions> GetAutocompleteOptions(Guid actorId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}