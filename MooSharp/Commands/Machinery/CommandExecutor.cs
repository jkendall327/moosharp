using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MooSharp.Commands.Presentation;

namespace MooSharp.Commands.Machinery;

/// <summary>
/// Routes commands to their appropriate handlers by utilising the visitor pattern.
/// </summary>
public class CommandExecutor(IServiceProvider services, ILogger<CommandExecutor> logger)
{
    // Pass control to the command...
    public Task<CommandResult> Handle(ICommand cmd, CancellationToken token = default)
        // The command calls back into the Handle method below, with proper generic type arguments
        => cmd.Dispatch(this, token);

    public async Task<CommandResult> Handle<TCommand>(TCommand cmd, CancellationToken token)
        where TCommand : ICommand
    {
        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            { "CommandType", cmd.GetType().Name }
        });

        logger.LogInformation("Beginning command execution");

        var handler = services.GetRequiredService<IHandler<TCommand>>();

        try
        {
            var result = await handler.Handle(cmd, token);

            logger.LogDebug("Command execution completed");

            return result;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error executing command");

            throw;
        }
    }
}