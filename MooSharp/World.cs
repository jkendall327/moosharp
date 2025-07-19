namespace MooSharp;

public class World
{
    private readonly List<RoomActor> _actors = new();

    public World()
    {
        _actors.Add(new RoomActor(new()
        {
            Id = 1,
            Name = "Atrium",
            Description = "A beautiful antechamber",
        }));
    }
}