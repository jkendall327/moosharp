- No reflection for accessing private methods/breaking encapsulation.
- No sync-over-async hacks. Use NotImplementedException to give up if you would be forced to use .GetAwaiter().GetResult() etc.
- Add tests for new code if viable.
- When writing docs, prefer concision. No emojis, no headers. Only use one level of indentation in lists. Write in simple markdown.

