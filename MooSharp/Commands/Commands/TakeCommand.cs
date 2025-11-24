using MooSharp.Messaging;

namespace MooSharp;

public class TakeCommand : CommandBase<TakeCommand>
{
    public required Player Player { get; init; }
    public required string Target { get; init; }
}

public class TakeCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } =
    [
        "take", "grab", "get"
    ];

    public string Description => "Pick up an item from the room. Usage: take <item>.";

    public ICommand Create(Player player, string args) =>
        new TakeCommand
        {
            Player = player,
            Target = args
        };
}

public class TakeHandler(World world) : IHandler<TakeCommand>
{
    public Task<CommandResult> Handle(TakeCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var player = cmd.Player;

        var currentLocation = world.GetPlayerLocation(player)
            ?? throw new InvalidOperationException("Player has no known current location.");

        var search = currentLocation.FindObjects(cmd.Target);

        switch (search.Status)
        {
            case SearchStatus.NotFound:
                result.Add(player, new ItemNotFoundEvent(cmd.Target));
                break;

            case SearchStatus.IndexOutOfRange:
                result.Add(player, new SystemMessageEvent($"You can't see a '{cmd.Target}' here."));
                break;

            case SearchStatus.Ambiguous:
                result.Add(player, new AmbiguousInputEvent(cmd.Target, search.Candidates));
                break;

            case SearchStatus.Found:
                var o = search.Match!;

                if (o.Owner is null)
                {
                    o.MoveTo(player);
                    result.Add(player, new ItemTakenEvent(o));
                }
                else if (o.Owner == player)
                {
                    result.Add(player, new ItemAlreadyInPossessionEvent(o));
                }
                else
                {
                    result.Add(player, new ItemOwnedByOtherEvent(o, o.Owner));
                }
                break;
        }

        return Task.FromResult(result);
    }
}