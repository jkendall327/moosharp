namespace MooSharp.Scripting;

public interface IScriptExecutor
{
    Task<ScriptResult> ExecuteAsync(ScriptExecutionContext context, CancellationToken ct = default);
}
