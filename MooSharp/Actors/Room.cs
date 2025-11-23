using Microsoft.Extensions.Logging;

namespace MooSharp;

public class Room
{
    public int Id { get; init; }
    public required string Name { get; init; } 
    public required string Slug { get; init; }
    public required string Description { get; init; } 
    public Dictionary<string, ObjectActor> Contents { get; } = new();
    public Dictionary<string, RoomActor> Exits { get; } = new();
    public List<PlayerActor> PlayersInRoom { get; } = new();

    public override string ToString() => Slug;
}

public class RoomActor(Room state, ILoggerFactory factory) : Actor<Room>(state, factory)
{
    private ILogger<RoomActor> _logger = factory.CreateLogger<RoomActor>();
    
    public int Id => _state.Id;
    public string Name => _state.Name; 
    public string Slug => _state.Slug;
    public string Description =>  _state.Description; 
    public IReadOnlyDictionary<string, RoomActor> Exits => _state.Exits;
    
    public async Task<List<PlayerActor>> GetPeopleInRoom()
    {
        var message = new RequestMessage<Room, List<PlayerActor>>(r => 
            Task.FromResult(r.PlayersInRoom.ToList()));

        return await Ask(message);
    }

    public Task RemovePlayer(PlayerActor player)
    {
        _logger.LogDebug("Removing player from room {Room}", _state.Name);

        _state.PlayersInRoom.Remove(player);

        return Task.CompletedTask;
    }

    public Task AddPlayer(PlayerActor player)
    {
        _logger.LogDebug("Adding player to room {Room}", _state.Name);

        _state.PlayersInRoom.Add(player);

        return Task.CompletedTask;
    }
}