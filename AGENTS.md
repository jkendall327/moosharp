## Project Overview

MooSharp is a C# .NET 9 MUD/MOO game engine where human players and LLM-powered agents interact through text commands. The web host uses ASP.NET Core with SignalR for real-time communication and SQLite for persistence.

Key directories:
- `MooSharp/Commands/` - Command system (definitions, handlers, parsing)
- `MooSharp/Actors/` - Domain entities (Player, Room, Object)
- `MooSharp/World/` - World state, clock, initialization
- `MooSharp/Agents/` - LLM agent integration
- `MooSharp/Messaging/` - Events and result broadcasting
- `MooSharp/Persistence/` - SQLite data layer
- `MooSharp.Web/` - ASP.NET host, SignalR hub, DI registration

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

## Agent System Architecture

The agent system allows LLM-powered AI players to participate in the game world alongside humans. Agents receive the same text feed as human players and must respond using the same command syntax - they have no privileged access or special capabilities. The system is designed around asynchronous message processing with careful concurrency control.

**Core components and flow:**

At startup, `AgentSpawner` loads agent definitions from `agents.json` (name, persona, LLM source, cooldowns). For each agent, it uses `AgentFactory` to construct an `AgentBrain` instance and sends a `RegisterAgentCommand` to create the agent's `Player` entity in the game world.

Each `AgentBrain` manages two concurrent loops. The processing loop reads incoming game messages (via `AgentPlayerConnection`) from an internal channel and feeds them to `AgentCore`. The volition loop periodically checks if the agent has been idle too long and, if so, injects a volition prompt to encourage spontaneous action.

`AgentCore` maintains the conversation history and enforces action cooldowns. When a message arrives, it appends it to the `ChatHistory`, checks cooldown, yields an `AgentThinkingCommand` (to update UI), makes the LLM call via `AgentResponseProvider`, appends the response to history, and yields a `WorldCommand` containing the agent's command text.

`AgentPromptProvider` builds the system prompt using a Handlebars template (`SystemPrompt.hbs`) that includes the agent's name, persona, and dynamically-generated help text from `CommandReference`. This ensures agents always know the current command vocabulary.

`AgentResponseProvider` handles multiple LLM backends (OpenAI, Anthropic, Gemini, OpenRouter) by creating transient Semantic Kernel instances. It logs timing and returns `ChatMessageContent` that gets parsed into game commands. The provider uses different connectors per source but maintains a unified interface.

`AgentPlayerConnection` implements `IPlayerConnection` just like SignalR connections for humans. When game events are formatted and sent to this connection, they flow into the agent's internal message channel rather than to a websocket. This abstraction allows the game engine to treat agents and humans identically.

Concurrency is managed with a semaphore in `AgentCore.ProcessMessageAsync()` that locks during LLM calls, preventing message reordering. The history is trimmed to the most recent N messages (configured in `AgentOptions`) to stay within context limits, but the system prompt is always preserved.

Action cooldowns prevent agents from spamming commands. Volition cooldowns determine how often the volition loop checks for idle agents. If an agent hasn't acted in over 5 minutes and passes the volition check, it receives the configured volition prompt (typically "What do you want to do next?") to encourage autonomous behavior.

The result is agents that behave as autonomous players: they receive game output as text, reason about it using their LLM, emit text commands, and participate in the world without special treatment.

## Coding Style

- No reflection for accessing private methods/breaking encapsulation.
- No sync-over-async hacks. Use NotImplementedException to give up if you would be forced to use .GetAwaiter()
  .GetResult() etc.
- When writing docs, prefer concision. No emojis, no headers. Only use one level of indentation in lists. Write in
  simple markdown.

## Tests

- Add tests for new code if viable.
- Do not write tests that assert based on log outputs.
- Do not write tests that make assertions based on raw strings (game message outputs, for example).
- Use NSubstitute for mocking purposes.
- If you make any test doubles, or general test-helper code, try to make it general and put it somewhere in the test
  project that other tests can make use of.
- Always run dotnet builds/dotnet test to check your work.
- If you are Codex CLI, you may have to run dotnet test etc. with elevated sandbox permissions; actively ask for
  permission in these cases.
- Always run tests against the whole solution. Do not filter down what tests you run: run all tests every time.
- Factor out repeated setup of dependencies into fields in your tests.

Elaboration on that last point - instead of defining a TimeProvider in every test in a class, do this:

```csharp
public class TreasureSpawnerServiceTests
{
    private readonly FakeTimeProvider _timeProvider = new();
```

Obviously you can't always do that. But prefer to do it that way when you can.
