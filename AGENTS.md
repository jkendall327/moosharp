## Project Overview

MooSharp is a C# .NET 10 MUD/MOO game engine where human players and LLM-powered agents interact through text commands. The web host uses ASP.NET Core with SignalR for real-time communication and SQLite for persistence.

Look in the `agent-docs` directory for more detailed explanations on the solution.

- `agent-system`: how the AI agents operate in the game engine.
- `creating-commands`: creating new game commands that players can use to interact with the world.
- `web-engine-integration`: how the web layer (ASP.NET Core/SignalR) interacts with the transport-agnostic game engine.

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
