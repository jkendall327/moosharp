namespace MooSharp;

public class World
{
    public List<RoomActor> Rooms { get; } = new();

    public World()
    {
        var room = new Room
        {
            Id = 1,
            Name = "Atrium",
            Description = "A beautiful antechamber",
        };

        var atrium = new RoomActor(room);

        var sideroom = new RoomActor(new()
        {
            Id = 2,
            Name = "Side-room",
            Description = "A small but clean break-room for drinking coffee",
            Exits =
            {
                {
                    "atrium", atrium
                }
            }
        });

        room.Exits.Add("side-room", sideroom);

        Rooms.Add(atrium);
        Rooms.Add(sideroom);

        room.Contents.Add("Cup",
            new(new()
            {
                Id = 4,
                Name = "Cup",
                Description = "It's a small, finely-wrought coffee cup.",
                Location = sideroom
            }));
    }
}