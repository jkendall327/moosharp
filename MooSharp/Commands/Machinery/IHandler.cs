using MooSharp.Messaging;

namespace MooSharp.Commands.Machinery;

/// <summary>
/// Represents a handler for a command, i.e. the behavioural component.
/// </summary>
public interface IHandler<in T> where T : ICommand
{
    Task<CommandResult> Handle(T command, CancellationToken cancellationToken = default);
}