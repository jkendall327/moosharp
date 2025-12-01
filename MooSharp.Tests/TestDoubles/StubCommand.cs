using MooSharp.Commands.Machinery;
using MooSharp.Messaging;

namespace MooSharp.Tests.TestDoubles;

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