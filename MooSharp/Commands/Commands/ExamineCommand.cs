using MooSharp.Messaging;

namespace MooSharp;

public class ExamineCommand : CommandBase<ExamineCommand>
{
    public required Player Player { get; init; }
    public required string Target { get; init; }
}

public class ExamineCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["examine", "view", "look"];

    public string Description => "Inspect yourself, an item, or the room. Usage: examine <target>.";

    public ICommand Create(Player player, string args)
        => new ExamineCommand
        {
            Player = player,
            Target = args
        };
}

public class ExamineHandler(World world) : IHandler<ExamineCommand>
{
    public Task<CommandResult> Handle(ExamineCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();

        var player = cmd.Player;

        if (string.IsNullOrWhiteSpace(cmd.Target))
        {
            var currentLocation = world.GetPlayerLocation(player)
                ?? throw new InvalidOperationException("Player has no known current location.");

            result.Add(player, new RoomDescriptionEvent(currentLocation.DescribeFor(player, useLongDescription: true)));

            return Task.FromResult(result);
        }

        if (cmd.Target is "me")
        {
            var inventory = player.Inventory
                .ToList();

            result.Add(player, new SelfExaminedEvent(player, inventory));

            return Task.FromResult(result);
        }

        var current = world.GetPlayerLocation(player)
            ?? throw new InvalidOperationException("Player has no known current location.");

        var search = current.FindObjects(cmd.Target);

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
                result.Add(player, new ObjectExaminedEvent(search.Match!));
                break;
        }

        return Task.FromResult(result);
    }
}