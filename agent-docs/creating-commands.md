## Command System Architecture

Commands use a three-piece pattern: Command (data), Definition (parsing), Handler (behavior).

Flow: Raw input → `CommandParser` matches verb → `ICommandDefinition.Create()` builds command → `CommandExecutor` dispatches via visitor pattern → `IHandler<T>` executes → `CommandResult` with events → formatters render to players.

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
    public ICommand Create(Player player, string args) => new DanceCommand { Player = player, Style = args };
}
```

3. **Handler** - Business logic:
```csharp
public class DanceHandler(World world) : IHandler<DanceCommand>
{
    public Task<CommandResult> Handle(DanceCommand cmd, CancellationToken ct = default)
    {
        var result = new CommandResult();
        var room = world.GetPlayerLocation(cmd.Player)!;
        result.Add(cmd.Player, new PlayerDancedEvent(cmd.Player, cmd.Style));
        result.BroadcastToAllButPlayer(room, cmd.Player, new PlayerDancedEvent(cmd.Player, cmd.Style));
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