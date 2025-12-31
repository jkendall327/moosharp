## Command System Architecture

Commands use a three-piece pattern: Command (data), Definition (parsing), Handler (behavior).

Flow: Raw input → `CommandParser` matches verb → `ICommandDefinition.TryCreateCommand()` builds command → `CommandExecutor` dispatches via visitor pattern → `IHandler<T>` executes → `CommandResult` with events → formatters render to players.

All implementations are auto-discovered via reflection in `ServiceCollectionExtensions`.

## Adding a New Command

Create a single file in `MooSharp/Commands/Commands/<Category>/` with four pieces:

1. **Command class** - Immutable data holder:
```csharp
public class DanceCommand : CommandBase<DanceCommand>
{
    public required Player Player { get; init; }
    public required string Style { get; init; }
}
```

2. **Definition** - Parsing and help text:
```csharp
public class DanceCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["dance"];
    public CommandCategory Category => CommandCategory.Social;
    public string Description => "Perform a dance. Usage: dance <style>.";

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        command = null;

        // "GetRemainingText" consumes the rest of the input as a single string
        var style = ctx.GetRemainingText();

        if (string.IsNullOrWhiteSpace(style))
        {
            return "Dance how?";
        }

        command = new DanceCommand { Player = ctx.Player, Style = style };
        return null; // Return null if successful, or an error string if parsing failed
    }
}
```

3. **Handler** - Business logic:
```csharp
public class DanceHandler(World.World world) : IHandler<DanceCommand>
{
    public Task<CommandResult> Handle(DanceCommand cmd, CancellationToken ct = default)
    {
        var result = new CommandResult();
        var room = world.GetLocationOrThrow(cmd.Player);

        var evt = new PlayerDancedEvent(cmd.Player, cmd.Style);

        // Show to the actor
        result.Add(cmd.Player, evt);

        // Show to everyone else in the room
        result.BroadcastToAllButPlayer(room, cmd.Player, evt);

        return Task.FromResult(result);
    }
}
```

4. **Event and formatter** - Message rendering:
```csharp
public record PlayerDancedEvent(Player Player, string Style) : IGameEvent;

public class PlayerDancedEventFormatter : IGameEventFormatter<PlayerDancedEvent>
{
    public string FormatForActor(PlayerDancedEvent e) => $"You dance {e.Style}.";
    public string FormatForObserver(PlayerDancedEvent e) => $"{e.Player.Username} dances {e.Style}.";
}
```

No registration needed - reflection discovers everything automatically.
