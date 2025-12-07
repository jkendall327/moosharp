using MooSharp.Actors.Players;
using MooSharp.Commands.Searching;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Commands.Parsing;

public class ArgumentBinder(TargetResolver resolver)
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
