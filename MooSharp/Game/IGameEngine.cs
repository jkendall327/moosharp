using MooSharp.Actors.Players;
using MooSharp.Features.Autocomplete;

namespace MooSharp.Game;

public interface IGameEngine
{
    event Action<Player> OnPlayerSpawned;
    event Action<Player> OnPlayerDespawned;

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