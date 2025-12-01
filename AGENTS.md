## Project Overview

MooSharp is a C# .NET 10 MUD/MOO game engine where human players and LLM-powered agents interact through text commands. The web host uses ASP.NET Core with SignalR for real-time communication and SQLite for persistence.

Key directories:
- `MooSharp/Commands/` - Command system (definitions, handlers, parsing)
- `MooSharp/Actors/` - Domain entities (Player, Room, Object)
- `MooSharp/World/` - World state, clock, initialization
- `MooSharp/Agents/` - LLM agent integration
- `MooSharp/Messaging/` - Events and result broadcasting
- `MooSharp/Persistence/` - SQLite data layer
- `MooSharp.Web/` - ASP.NET host, SignalR hub, DI registration

Look in the `agent-docs` directory for more detailed explanations on how to add new commands, how the AI agent subsystem works, etc.

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
