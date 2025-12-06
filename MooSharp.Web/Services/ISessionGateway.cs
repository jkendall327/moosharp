using MooSharp.Infrastructure;
using MooSharp.Messaging;

namespace MooSharp.Gateway;

/// <summary>
/// Manages the lifecycle of active sessions.
/// Handles the mapping between ActorIds (Game) and OutputChannels (Transport).
/// Owns the "Linkdead" timer logic.
/// </summary>
public interface ISessionGateway
{
    /// <summary>
    /// Called by SignalR Hub (OnConnected) or AgentSpawner.
    /// 1. Cancels any pending "Linkdead" cleanup timers for this user.
    /// 2. Registers the new output channel for this actor.
    /// 3. If the actor isn't in the engine yet, asks the Engine to spawn them.
    /// </summary>
    Task OnSessionStartedAsync(Guid actorId, IOutputChannel channel);

    /// <summary>
    /// Called by SignalR Hub (OnDisconnected).
    /// 1. Marks the session as "Linkdead".
    /// 2. Starts a background timer (e.g., 5 mins). 
    ///    If the timer expires before a reconnect, calls Engine.DespawnActorAsync.
    /// </summary>
    Task OnSessionEndedAsync(Guid actorId);

    /// <summary>
    /// Called by the GameEngine (e.g., user types /quit).
    /// 1. Immediately closes the output channel (if active).
    /// 2. Skips the linkdead timer and immediately calls Engine.DespawnActorAsync.
    /// </summary>
    Task ForceDisconnectAsync(Guid actorId);

    /// <summary>
    /// Called by the Game Engine to send text to a specific actor.
    /// The Gateway looks up the active IOutputChannel for this ActorId and forwards the message.
    /// If the user is Linkdead, this might buffer the message or drop it.
    /// </summary>
    Task DispatchToActorAsync(Guid actorId, IGameEvent gameEvent);
    
    /// <summary>
    /// Sends a message to everyone currently connected (e.g. shutdown announcements).
    /// </summary>
    Task BroadcastAsync(IGameEvent gameEvent);
}