using MooSharp.Messaging;

namespace MooSharp;

public static class WorldExtensions
{
    extension(World world)
    {
        public Room GetLocationOrThrow(Player player)
        {
            return world.GetPlayerLocation(player)
                   ?? throw new InvalidOperationException($"Player {player.Username} has no location.");
        }

        public bool TryGetLocation(Player player, out Room? room, out CommandResult? error)
        {
            room = world.GetPlayerLocation(player);
            if (room is null)
            {
                error = new();
                error.Add(player, new SystemMessageEvent("You are floating in the void."));
                return false;
            }

            error = null;
            return true;
        }
    }
}
