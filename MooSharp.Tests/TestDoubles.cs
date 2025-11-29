using MooSharp;
using MooSharp.Messaging;

namespace MooSharp.Tests;

public sealed class StubCommand : ICommand
{
    public StubCommand(string args)
    {
        Args = args;
    }

    public string Args { get; }

    public Task<CommandResult> Dispatch(CommandExecutor executor, CancellationToken token)
        => Task.FromResult(new CommandResult());
}

public sealed class TestPlayerConnection : IPlayerConnection
{
    public string Id { get; } = Guid.NewGuid().ToString();

    public List<string> Messages { get; } = new();

    public Task SendMessageAsync(string message, CancellationToken ct = default)
    {
        Messages.Add(message);
        return Task.CompletedTask;
    }
}
