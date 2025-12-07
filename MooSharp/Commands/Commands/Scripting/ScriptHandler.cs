using MooSharp.Commands.Machinery;
using MooSharp.Commands.Presentation;
using MooSharp.Scripting;

namespace MooSharp.Commands.Commands.Scripting;

public class ScriptHandler(IScriptExecutor executor, World.World world) : IHandler<ScriptCommand>
{
    public async Task<CommandResult> Handle(ScriptCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var room = world.GetLocationOrThrow(cmd.Player);

        var context = new ScriptExecutionContext(
            cmd.Script.LuaCode,
            cmd.TargetObject,
            cmd.Player,
            room,
            cmd.VerbName,
            cmd.Arguments);

        var scriptResult = await executor.ExecuteAsync(context, cancellationToken);

        if (!scriptResult.Success)
        {
            result.Add(cmd.Player, new ScriptErrorEvent(scriptResult.ErrorMessage!));
            return result;
        }

        // Convert script messages to game events
        if (scriptResult.Messages is not null)
        {
            foreach (var msg in scriptResult.Messages)
            {
                result.Add(msg.Recipient, new ScriptOutputEvent(msg.Text));
            }
        }

        return result;
    }
}
