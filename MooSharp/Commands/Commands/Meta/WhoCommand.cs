using MooSharp.Actors.Players;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Presentation;

namespace MooSharp.Commands.Commands.Meta;

public class WhoCommand : CommandBase<WhoCommand>
{
    public required Player Player { get; init; }
}

public class WhoCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["who"];

    public CommandCategory Category => CommandCategory.Meta;
    public string Description => "List all players currently online.";

    public ICommand Create(Player player, string args) => new WhoCommand
    {
        Player = player
    };
}

public class WhoHandler(World.World world) : IHandler<WhoCommand>
{
    public Task<CommandResult> Handle(WhoCommand cmd, CancellationToken cancellationToken = default)
    {
        var usernames = world.Players
            .Values
            .Select(player => player.Username)
            .OrderBy(username => username, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var result = new CommandResult();

        result.Add(cmd.Player, new OnlinePlayersEvent(usernames));

        return Task.FromResult(result);
    }
}

public record OnlinePlayersEvent(IReadOnlyCollection<string> Usernames) : IGameEvent;

public class OnlinePlayersEventFormatter : IGameEventFormatter<OnlinePlayersEvent>
{
    public string FormatForActor(OnlinePlayersEvent gameEvent)
    {
        if (gameEvent.Usernames.Count is 0)
        {
            return "No one is online.";
        }

        return $"Players online: {string.Join(", ", gameEvent.Usernames)}";
    }

    public string FormatForObserver(OnlinePlayersEvent gameEvent) => FormatForActor(gameEvent);
}
