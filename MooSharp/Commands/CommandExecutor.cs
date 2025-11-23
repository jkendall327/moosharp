using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MooSharp;

public class CommandExecutor(IServiceProvider serviceProvider, ILogger<CommandExecutor> logger)
{
    public async Task Handle(ICommand? cmd, StringBuilder sb, CancellationToken token = default)
    {
        if (cmd is null)
        {
            logger.LogDebug("Failed to parse player input as command");
            sb.AppendLine("That command wasn't recognized. Use 'move' to go between locations.");

            return;
        }

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

        await task;
    }

    private async Task Handle<TCommand>(TCommand command, StringBuilder buffer, CancellationToken token = default)
        where TCommand : ICommand
    {
        var handler = serviceProvider.GetRequiredService<IHandler<TCommand>>();

        await handler.Handle(command, buffer, token);
    }
}