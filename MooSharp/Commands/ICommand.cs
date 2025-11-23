using MooSharp.Messaging;

namespace MooSharp;

/// <summary>
/// An action that players can take in the game.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Tells the command executor what generic type to use for finding the handler of this command.
    /// </summary>
    Task<CommandResult> Dispatch(CommandExecutor executor, CancellationToken token);
}

public abstract class CommandBase<TSelf> : ICommand
    where TSelf : CommandBase<TSelf>
{
    public Task<CommandResult> Dispatch(CommandExecutor executor, CancellationToken token = default)
        => executor.Handle((TSelf)this, token);
}