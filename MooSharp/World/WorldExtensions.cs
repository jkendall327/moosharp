using MooSharp.Messaging;

namespace MooSharp;

public static class WorldExtensions
{
    public static Room GetLocationOrThrow(this World world, Player player)
    {
        return world.GetPlayerLocation(player)
               ?? throw new InvalidOperationException($"Player {player.Username} has no location.");
    }

    public static bool TryGetLocation(this World world, Player player, out Room? room, out CommandResult? error)
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
