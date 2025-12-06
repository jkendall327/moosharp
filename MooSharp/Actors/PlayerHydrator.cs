using MooSharp.Data.Dtos;

namespace MooSharp.Actors;

public class PlayerHydrator(World.World world)
{
    /// <summary>
    /// Given a player, refreshes their inventory and current location from the database DTO.
    /// </summary>
    public Task<Player> RehydrateAsync(PlayerDto dto)
    {
        var player = new Player
        {
            Username = dto.Username
        };
        
        foreach (var item in dto.Inventory)
        {
            var obj = new Object
            {
                Id = new(Guid.Parse(item.Id)),
                Name = item.Name,
                Description = item.Description,
                Flags = (ObjectFlags) item.Flags,
                KeyId = item.KeyId,
                CreatorUsername = item.CreatorUsername
            };

            if (!string.IsNullOrWhiteSpace(item.TextContent))
            {
                obj.WriteText(item.TextContent);
            }

            obj.MoveTo(player);
        }

        var startingRoom = world.Rooms.TryGetValue(new(dto.CurrentLocation), out var r)
            ? r
            : world.GetDefaultRoom();

        world.MovePlayer(player, startingRoom);

        return Task.FromResult(player);
    }
}