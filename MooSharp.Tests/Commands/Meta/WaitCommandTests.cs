using MooSharp.Actors.Players;
using MooSharp.Commands.Commands.Meta;
using MooSharp.Commands.Machinery;
using MooSharp.Commands.Presentation;
using MooSharp.Tests.Handlers;
using MooSharp.Tests.TestDoubles;
using NSubstitute;

namespace MooSharp.Tests.Commands.Meta;

public class WaitCommandTests
{
    private readonly World.World _world;
    private readonly Player _player;
    private readonly Player _observer;
    private readonly WaitHandler _handler;

    public WaitCommandTests()
    {
        var origin = HandlerTestHelpers.CreateRoom("origin");

        var repository = new InMemoryWorldRepository();
        _world = new World.World(repository, Microsoft.Extensions.Logging.Abstractions.NullLogger<World.World>.Instance);
        _world.Initialize([origin]);

        _player = HandlerTestHelpers.CreatePlayer("Tester");
        _world.RegisterPlayer(_player);
        _world.MovePlayer(_player, origin);

        _observer = HandlerTestHelpers.CreatePlayer("Observer");
        _world.RegisterPlayer(_observer);
        _world.MovePlayer(_observer, origin);

        _handler = new WaitHandler(_world);
    }

    [Fact]
    public async Task Handle_ReturnsTimePassesForActor()
    {
        var cmd = new WaitCommand { Player = _player };
        var result = await _handler.Handle(cmd);

        var actorEvents = result.Messages.Where(m => m.Player == _player).Select(m => m.Event).ToList();
        Assert.Single(actorEvents);

        var formatter = new PlayerWaitsEventFormatter();
        Assert.Equal("Time passes...", formatter.FormatForActor((PlayerWaitsEvent)actorEvents[0]));
    }

    [Fact]
    public async Task Handle_ReturnsWaitMessageForObserver()
    {
        var cmd = new WaitCommand { Player = _player };
        var result = await _handler.Handle(cmd);

        var observerEvents = result.Messages.Where(m => m.Player == _observer).Select(m => m.Event).ToList();
        Assert.Single(observerEvents);

        var formatter = new PlayerWaitsEventFormatter();
        Assert.Equal("Tester waits.", formatter.FormatForObserver((PlayerWaitsEvent)observerEvents[0]));
    }
}
