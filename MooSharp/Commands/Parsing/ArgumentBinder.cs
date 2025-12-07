using MooSharp.Actors.Players;
using MooSharp.Actors.Rooms;
using MooSharp.Commands.Searching;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Commands.Parsing;

public class ArgumentBinder(TargetResolver resolver, World.World world)
{
    // Binds a token to an object in the player's inventory
    public BindingResult<Object> BindInventoryItem(ParsingContext ctx)
    {
        if (ctx.IsFinished)
        {
            return BindingResult<Object>.Failure("You didn't specify an item.");
        }

        var token = ctx.Pop()!;
        var search = resolver.FindObjects(ctx.Player.Inventory, token);

        return search.Status switch
        {
            SearchStatus.Found => BindingResult<Object>.Success(search.Match!),
            SearchStatus.NotFound => BindingResult<Object>.Failure($"You aren't carrying a '{token}'."),
            SearchStatus.Ambiguous => BindingResult<Object>.Failure($"Which '{token}' do you mean?"),
            var _ => BindingResult<Object>.Failure("Invalid item.")
        };
    }

    /// <summary>
    /// Attempts to find a player currently connected to the game (Global search).
    /// Used for 'Whisper'.
    /// </summary>
    public BindingResult<Player> BindOnlinePlayer(ParsingContext ctx)
    {
        if (ctx.IsFinished)
        {
            return BindingResult<Player>.Failure("You didn't specify a player.");
        }

        var token = ctx.Pop()!;

        // Exact match or Case-insensitive match on Username
        var target = world
            .GetActivePlayers()
            .FirstOrDefault(p => p.Username.Equals(token, StringComparison.OrdinalIgnoreCase));

        if (target is null)
        {
            return BindingResult<Player>.Failure($"Player '{token}' is not online.");
        }

        return BindingResult<Player>.Success(target);
    }

    public BindingResult<string> BindChannelName(ParsingContext ctx)
    {
        if (ctx.IsFinished)
        {
            return BindingResult<string>.Failure("You didn't specify a channel.");
        }

        var token = ctx.Pop()!;

        if (!Features.Chats.ChatChannels.IsValid(token))
        {
            return BindingResult<string>.Failure($"Channel '{token}' does not exist.");
        }

        return BindingResult<string>.Success(Features.Chats.ChatChannels.Normalize(token));
    }

    /// <summary>
    /// Attempts to find an item in the Inventory OR the Room.
    /// Used for 'Examine', 'Read'.
    /// </summary>
    public BindingResult<Object> BindNearbyObject(ParsingContext ctx)
    {
        if (ctx.IsFinished)
        {
            return BindingResult<Object>.Failure("You didn't specify an item.");
        }

        var token = ctx.Pop()!;

        // 1. Check Inventory
        var invSearch = resolver.FindObjects(ctx.Player.Inventory, token);

        if (invSearch.Status == SearchStatus.Found)
        {
            return BindingResult<Object>.Success(invSearch.Match!);
        }

        // 2. Check Room
        var roomSearch = resolver.FindObjects(ctx.Room.Contents, token);

        return roomSearch.Status switch
        {
            SearchStatus.Found => BindingResult<Object>.Success(roomSearch.Match!),
            SearchStatus.NotFound => BindingResult<Object>.Failure($"You don't see a '{token}' here."),
            SearchStatus.Ambiguous => BindingResult<Object>.Failure($"Which '{token}' do you mean?"),
            SearchStatus.IndexOutOfRange => BindingResult<Object>.Failure($"You don't see that many '{token}'s."),
            _ => BindingResult<Object>.Failure("Invalid item.")
        };
    }

    public BindingResult<Room> BindExitInRoom(ParsingContext ctx)
    {
        if (ctx.IsFinished)
        {
            return BindingResult<Room>.Failure("You didn't specify an exit.");
        }

        var token = ctx.Pop()!;
        
        var exits = ctx.Room.Exits;

        if (exits.TryGetValue(token, out var id))
        {
            var room = world.Rooms[id];
            return BindingResult<Room>.Success(room);
        }
        
        return BindingResult<Room>.Failure("Exit not found.");
    }

    // Binds a token to a player in the room
    public BindingResult<Player> BindPlayerInRoom(ParsingContext ctx)
    {
        if (ctx.IsFinished)
        {
            return BindingResult<Player>.Failure("You didn't specify a person.");
        }

        var token = ctx.Pop()!;

        // Simple linear search for now, could use TargetResolver logic here too
        var target =
            ctx.Room.PlayersInRoom.FirstOrDefault(p => p.Username.Equals(token, StringComparison.OrdinalIgnoreCase));

        return target is not null
            ? BindingResult<Player>.Success(target)
            : BindingResult<Player>.Failure($"You don't see '{token}' here.");
    }

    // Consumes a preposition (syntactic sugar)
    public bool ConsumePreposition(ParsingContext ctx, string preposition)
    {
        if (ctx
                .Peek()
                ?.Equals(preposition, StringComparison.OrdinalIgnoreCase) == true)
        {
            ctx.Pop();

            return true;
        }

        return false;
    }
}