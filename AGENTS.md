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
