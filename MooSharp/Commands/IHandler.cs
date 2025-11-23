using System.Text;
using MooSharp.Messaging;

namespace MooSharp;

public interface IHandler<in T> where T : ICommand
{
    Task<CommandResult> Handle(T command, CancellationToken cancellationToken = default);
}