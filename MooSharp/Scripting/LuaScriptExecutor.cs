using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoonSharp.Interpreter;
using MooSharp.Infrastructure;
using MooSharp.Scripting.Api;

namespace MooSharp.Scripting;

public class LuaScriptExecutor(
    IOptions<LuaScriptOptions> options,
    MooSharpMetrics metrics,
    ILogger<LuaScriptExecutor> logger) : IScriptExecutor
{
    private readonly LuaScriptOptions _options = options.Value;

    static LuaScriptExecutor()
    {
        // Register user data types for MoonSharp
        UserData.RegisterType<LuaGameApi>();
        UserData.RegisterType<LuaSelfApi>();
        UserData.RegisterType<LuaActorApi>();
        UserData.RegisterType<LuaRoomApi>();
    }

    public async Task<ScriptResult> ExecuteAsync(ScriptExecutionContext context, CancellationToken ct = default)
    {
        var script = CreateSandboxedScript();
        var gameApi = new LuaGameApi(context);
        var selfApi = new LuaSelfApi(context.TargetObject);
        var actorApi = new LuaActorApi(context.Actor, context.Location);
        var roomApi = new LuaRoomApi(context.Location);

        RegisterApis(script, gameApi, selfApi, actorApi, roomApi, context);

        script.Options.DebugPrint = s => logger.LogDebug("Lua print: {Message}", s);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(_options.TimeoutMilliseconds);

            // Execute the script with timeout protection
            var executeTask = Task.Run(() => script.DoString(context.LuaCode), cts.Token);

            await executeTask.WaitAsync(cts.Token);

            stopwatch.Stop();

            logger.LogDebug(
                "Script '{Verb}' on '{Object}' completed successfully",
                context.VerbName,
                context.TargetObject.Name);

            metrics.RecordVerbExecution(context.VerbName, stopwatch.Elapsed.TotalMilliseconds, success: true);

            return ScriptResult.Ok(gameApi.GetMessages());
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            metrics.RecordVerbExecution(context.VerbName, stopwatch.Elapsed.TotalMilliseconds, success: false);

            logger.LogWarning(
                "Script '{Verb}' on '{Object}' timed out after {Timeout}ms",
                context.VerbName,
                context.TargetObject.Name,
                _options.TimeoutMilliseconds);

            return ScriptResult.Error("Script execution timed out.");
        }
        catch (ScriptRuntimeException ex)
        {
            stopwatch.Stop();
            metrics.RecordVerbExecution(context.VerbName, stopwatch.Elapsed.TotalMilliseconds, success: false);

            logger.LogWarning(
                ex,
                "Script '{Verb}' on '{Object}' failed with runtime error",
                context.VerbName,
                context.TargetObject.Name);

            return ScriptResult.Error($"Script error: {ex.DecoratedMessage}");
        }
        catch (SyntaxErrorException ex)
        {
            stopwatch.Stop();
            metrics.RecordVerbExecution(context.VerbName, stopwatch.Elapsed.TotalMilliseconds, success: false);

            logger.LogWarning(
                ex,
                "Script '{Verb}' on '{Object}' has syntax error",
                context.VerbName,
                context.TargetObject.Name);

            return ScriptResult.Error($"Script syntax error: {ex.DecoratedMessage}");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            metrics.RecordVerbExecution(context.VerbName, stopwatch.Elapsed.TotalMilliseconds, success: false);

            logger.LogError(
                ex,
                "Script '{Verb}' on '{Object}' failed with unexpected error",
                context.VerbName,
                context.TargetObject.Name);

            return ScriptResult.Error("An unexpected error occurred while running the script.");
        }
    }

    private static Script CreateSandboxedScript()
    {
        var script = new Script(CoreModules.Preset_SoftSandbox);

        // Remove potentially dangerous modules/globals
        script.Globals.Remove("io");
        script.Globals.Remove("os");
        script.Globals.Remove("debug");
        script.Globals.Remove("load");
        script.Globals.Remove("loadfile");
        script.Globals.Remove("dofile");
        script.Globals.Remove("require");
        script.Globals.Remove("loadstring");

        return script;
    }

    private static void RegisterApis(
        Script script,
        LuaGameApi gameApi,
        LuaSelfApi selfApi,
        LuaActorApi actorApi,
        LuaRoomApi roomApi,
        ScriptExecutionContext context)
    {
        script.Globals["game"] = UserData.Create(gameApi);
        script.Globals["self"] = UserData.Create(selfApi);
        script.Globals["actor"] = UserData.Create(actorApi);
        script.Globals["room"] = UserData.Create(roomApi);
        script.Globals["verb"] = context.VerbName;
        script.Globals["args"] = context.Arguments;
    }
}
