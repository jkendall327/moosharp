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