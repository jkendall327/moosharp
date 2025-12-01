using MooSharp.Actors;

namespace MooSharp.World;

public static class WorldExtensions
{
    extension(World world)
    {
        public Room GetLocationOrThrow(Player player)
        {
            return world.GetPlayerLocation(player)
                   ?? throw new InvalidOperationException($"Player {player.Username} has no location.");
        }
    }
}
