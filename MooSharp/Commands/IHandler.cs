using System.Text;

namespace MooSharp;

public interface IHandler<in T> where T : ICommand
{
    Task Handle(T command, StringBuilder buffer, CancellationToken cancellationToken = default);
}