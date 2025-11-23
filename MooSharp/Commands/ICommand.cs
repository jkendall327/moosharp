using MooSharp.Messaging;

namespace MooSharp;

public interface ICommand
{
    /// <summary>
    /// Tells the command executor what generic type to use for finding the handler of this command.
    /// </summary>
    Task<CommandResult> Dispatch(CommandExecutor executor, CancellationToken token);
}