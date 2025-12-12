using MooSharp.Actors.Objects;
using MooSharp.Actors.Players;
using MooSharp.Commands.Commands.Scripting;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Parsing;
using MooSharp.Commands.Presentation;
using MooSharp.Scripting;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Commands.Commands.Items;

public class UseCommand : CommandBase<UseCommand>
{
    public required Player Player { get; init; }

    public required Object Target { get; init; }
}

public class UseCommandDefinition : ICommandDefinition
{
    public IReadOnlyCollection<string> Verbs { get; } = ["use"];

    public string Description => "Use an object. Usage: 'use <item>'.";

    public CommandCategory Category => CommandCategory.General;

    public string? TryCreateCommand(ParsingContext ctx, ArgumentBinder binder, out ICommand? command)
    {
        command = null;

        var bind = binder.BindNearbyObject(ctx);

        if (!bind.IsSuccess)
        {
            return bind.ErrorMessage;
        }

        command = new UseCommand
        {
            Player = ctx.Player,
            Target = bind.Value
        };

        return null;
    }
}

public class UseHandler(IScriptExecutor executor, World.World world) : IHandler<UseCommand>
{
    public async Task<CommandResult> Handle(UseCommand cmd, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var target = cmd.Target;

        var scriptSelection = SelectScript(target);

        if (scriptSelection.Script is null)
        {
            result.Add(cmd.Player, new UseFailedEvent(target, target.Verbs.VerbNames.ToList()));

            return result;
        }

        var room = world.GetLocationOrThrow(cmd.Player);

        var context = new ScriptExecutionContext(
            scriptSelection.Script.LuaCode,
            target,
            cmd.Player,
            room,
            scriptSelection.VerbName,
            []);

        var scriptResult = await executor.ExecuteAsync(context, cancellationToken);

        if (!scriptResult.Success)
        {
            result.Add(cmd.Player, new ScriptErrorEvent(scriptResult.ErrorMessage!));

            return result;
        }

        if (scriptResult.Messages is not null)
        {
            foreach (var msg in scriptResult.Messages)
            {
                result.Add(msg.Recipient, new ScriptOutputEvent(msg.Text));
            }
        }

        return result;
    }

    private static (VerbScript? Script, string VerbName) SelectScript(Object target)
    {
        if (target.Verbs.TryGetVerb("use", out var useScript) && useScript is not null)
        {
            return (useScript, "use");
        }

        if (target.Verbs.Count == 1)
        {
            var singleScript = target.Verbs.Verbs.Single();

            return (singleScript, singleScript.VerbName);
        }

        return (null, string.Empty);
    }
}

public record UseFailedEvent(Object Target, IReadOnlyCollection<string> Verbs) : IGameEvent;

public class UseFailedEventFormatter : IGameEventFormatter<UseFailedEvent>
{
    public string FormatForActor(UseFailedEvent gameEvent)
    {
        if (gameEvent.Verbs.Count == 0)
        {
            return "You can't use that. It doesn't seem to do anything.";
        }

        var verbList = string.Join(", ", gameEvent.Verbs);

        return $"You can't use that. Available verbs: {verbList}.";
    }

    public string? FormatForObserver(UseFailedEvent gameEvent) => null;
}
