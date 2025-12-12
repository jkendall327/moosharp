using MooSharp.Actors.Objects;
using MooSharp.Commands.Commands.Items;
using MooSharp.Commands.Commands.Scripting;
using MooSharp.Commands.Presentation;
using MooSharp.Scripting;
using MooSharp.Tests.TestDoubles;
using NSubstitute;
using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Tests.Handlers;

public class UseHandlerTests
{
    [Fact]
    public async Task UseHandler_RunsUseVerbWhenPresent()
    {
        var room = HandlerTestHelpers.CreateRoom("room");
        var world = await HandlerTestHelpers.CreateWorld(room);

        var player = HandlerTestHelpers.CreatePlayer();
        world.MovePlayer(player, room);

        var target = new Object
        {
            Name = "Control Panel",
            Description = "A dusty console"
        };

        target.Verbs["use"] = VerbScript.Create("use", "return");
        target.Verbs["press"] = VerbScript.Create("press", "return");
        target.MoveTo(room);

        var executor = Substitute.For<IScriptExecutor>();
        executor
            .ExecuteAsync(Arg.Any<ScriptExecutionContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ScriptResult.Ok([new ScriptMessage(player, "done")])));

        var handler = new UseHandler(executor, world);

        var result = await handler.Handle(new UseCommand
        {
            Player = player,
            Target = target
        });

        await executor
            .Received(1)
            .ExecuteAsync(
                Arg.Is<ScriptExecutionContext>(ctx => ctx.VerbName == "use" && ReferenceEquals(ctx.TargetObject, target)),
                Arg.Any<CancellationToken>());

        var message = Assert.Single(result.Messages);
        Assert.IsType<ScriptOutputEvent>(message.Event);
    }

    [Fact]
    public async Task UseHandler_RunsSingleVerbWhenUseMissing()
    {
        var room = HandlerTestHelpers.CreateRoom("room");
        var world = await HandlerTestHelpers.CreateWorld(room);

        var player = HandlerTestHelpers.CreatePlayer();
        world.MovePlayer(player, room);

        var target = new Object
        {
            Name = "Lever",
            Description = "A rusty lever"
        };

        target.Verbs["pull"] = VerbScript.Create("pull", "return");
        target.MoveTo(room);

        var executor = Substitute.For<IScriptExecutor>();
        executor
            .ExecuteAsync(Arg.Any<ScriptExecutionContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ScriptResult.Ok()));

        var handler = new UseHandler(executor, world);

        await handler.Handle(new UseCommand
        {
            Player = player,
            Target = target
        });

        await executor
            .Received(1)
            .ExecuteAsync(
                Arg.Is<ScriptExecutionContext>(ctx => ctx.VerbName == "pull" && ReferenceEquals(ctx.TargetObject, target)),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UseHandler_ReportsAvailableVerbsWhenNoMatch()
    {
        var repository = new InMemoryWorldRepository();
        var room = HandlerTestHelpers.CreateRoom("room");
        var world = await HandlerTestHelpers.CreateWorld(repository, room);

        var player = HandlerTestHelpers.CreatePlayer();
        world.MovePlayer(player, room);

        var target = new Object
        {
            Name = "Panel",
            Description = "A metal panel"
        };

        target.Verbs["press"] = VerbScript.Create("press", "return");
        target.Verbs["activate"] = VerbScript.Create("activate", "return");
        target.MoveTo(room);

        var executor = Substitute.For<IScriptExecutor>();

        var handler = new UseHandler(executor, world);

        var result = await handler.Handle(new UseCommand
        {
            Player = player,
            Target = target
        });

        await executor
            .DidNotReceive()
            .ExecuteAsync(Arg.Any<ScriptExecutionContext>(), Arg.Any<CancellationToken>());

        var message = Assert.Single(result.Messages);
        var evt = Assert.IsType<UseFailedEvent>(message.Event);
        Assert.Same(target, evt.Target);
        Assert.Contains("press", evt.Verbs);
        Assert.Contains("activate", evt.Verbs);
    }
}
