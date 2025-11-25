using MooSharp;
using MooSharp.Messaging;

namespace MooSharp.Tests;

public sealed class RecordingDefinition : ICommandDefinition
{
    public RecordingDefinition(params string[] verbs)
    {
        Verbs = verbs;
    }

    public IReadOnlyCollection<string> Verbs { get; }

    public string Description => "test";

    public Player? LastPlayer { get; private set; }

    public string? LastArgs { get; private set; }

    public ICommand Create(Player player, string args)
    {
        LastPlayer = player;
        LastArgs = args;
        return new StubCommand(args);
    }
}

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

    public Task SendMessageAsync(string message)
    {
        Messages.Add(message);
        return Task.CompletedTask;
    }
}
