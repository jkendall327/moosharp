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
    public int Id => State.Id;
    public string Name => State.Name; 
    public string Slug => State.Slug;
    public string Description =>  State.Description; 
    public IReadOnlyDictionary<string, RoomActor> Exits => State.Exits;
    
    public async Task<List<PlayerActor>> GetPeopleInRoom()
    {
        var message = new RequestMessage<Room, List<PlayerActor>>(r => 
            Task.FromResult(r.PlayersInRoom.ToList()));

        return await Ask(message);
    }
}