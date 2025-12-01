using MooSharp.Commands.Machinery;
using MooSharp.Messaging;

namespace MooSharp.Tests.TestDoubles;

public sealed class StubCommand(string args) : ICommand
{
    public string Args { get; } = args;

    public Task<CommandResult> Dispatch(CommandExecutor executor, CancellationToken token)
        => Task.FromResult(new CommandResult());
}