namespace MooSharp.Messaging;

public interface IPlayerConnection
{
    // The unique ID used for routing (SignalR ConnectionId or Agent Guid)
    string Id { get; } 
    
    // How the game sends text to this entity
    Task SendMessageAsync(string message);
}