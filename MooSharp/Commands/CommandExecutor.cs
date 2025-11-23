using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MooSharp.Messaging;

namespace MooSharp;

public class CommandExecutor(IServiceProvider serviceProvider, ILogger<CommandExecutor> logger)
{
    public async Task<CommandResult> Handle(ICommand cmd, StringBuilder sb, CancellationToken token = default)
    {
        logger.LogDebug("Parsed input to command {CommandType}", cmd.GetType().Name);

        // This switch expression is just here so the compiler determines the type of the cmd,
        // and by extension, the generic argument to executor.Handle<T>().
        // This lets the executor get the correct handler implementation from DI.
        var task = cmd switch
        {
            ExamineCommand e => Handle(e, sb, token),
            MoveCommand m => Handle(m, sb, token),
            TakeCommand t => Handle(t, sb, token),
            _ => throw new ArgumentOutOfRangeException(nameof(cmd), "Unrecognised command type")
        };

        return await task;
    }

    private async Task<CommandResult> Handle<TCommand>(TCommand command, StringBuilder buffer, CancellationToken token = default)
        where TCommand : ICommand
    {
        var handler = serviceProvider.GetRequiredService<IHandler<TCommand>>();

        return await handler.Handle(command, buffer, token);
    }
}