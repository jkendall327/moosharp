using NSubstitute;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using MooSharp.Infrastructure;
using MooSharp.Messaging;
using MooSharp.Persistence;

namespace MooSharp.Tests;

public class WorldClockServiceTests
{
    [Fact]
    public async Task TriggerTickAsync_DoesNotBroadcastBeforePeriodDurationElapsed()
    {
        var world = CreateWorldWithPlayers(out var _);
        var presenter = Substitute.For<IGameMessagePresenter>();
        var timeProvider = new FakeTimeProvider();

        var clock = CreateWorldClock(world, presenter, timeProvider, dayPeriodMinutes: 10);

        // Advance only 5 minutes - not enough
        timeProvider.Advance(TimeSpan.FromMinutes(5));
        await clock.TriggerTickAsync(CancellationToken.None);

        presenter.DidNotReceive().Present(Arg.Any<GameMessage>());
    }

    [Fact]
    public async Task TriggerTickAsync_BroadcastsWhenPeriodDurationElapsed()
    {
        var world = CreateWorldWithPlayers(out var connections);
        var presenter = Substitute.For<IGameMessagePresenter>();
        presenter.Present(Arg.Any<GameMessage>()).Returns("event");
        var timeProvider = new FakeTimeProvider();

        var clock = CreateWorldClock(world, presenter, timeProvider, dayPeriodMinutes: 10);

        // Advance 10 minutes - triggers period change
        timeProvider.Advance(TimeSpan.FromMinutes(10));
        await clock.TriggerTickAsync(CancellationToken.None);

        foreach (var connection in connections)
        {
            await connection.Received(1).SendMessageAsync(Arg.Any<string>());
        }
    }

    [Fact]
    public async Task TriggerTickAsync_AdvancesDayPeriod()
    {
        var world = CreateWorldWithPlayers(out var _);
        world.CurrentDayPeriod = DayPeriod.Morning;

        var presenter = Substitute.For<IGameMessagePresenter>();
        presenter.Present(Arg.Any<GameMessage>()).Returns("event");
        var timeProvider = new FakeTimeProvider();

        var clock = CreateWorldClock(world, presenter, timeProvider, dayPeriodMinutes: 10);

        timeProvider.Advance(TimeSpan.FromMinutes(10));
        await clock.TriggerTickAsync(CancellationToken.None);

        Assert.Equal(DayPeriod.Afternoon, world.CurrentDayPeriod);
    }

    [Fact]
    public async Task TriggerTickAsync_CyclesThroughAllPeriods()
    {
        var world = CreateWorldWithPlayers(out var _);
        world.CurrentDayPeriod = DayPeriod.Night;

        var presenter = Substitute.For<IGameMessagePresenter>();
        presenter.Present(Arg.Any<GameMessage>()).Returns("event");
        var timeProvider = new FakeTimeProvider();

        var clock = CreateWorldClock(world, presenter, timeProvider, dayPeriodMinutes: 10);

        // Night should cycle back to Dawn
        timeProvider.Advance(TimeSpan.FromMinutes(10));
        await clock.TriggerTickAsync(CancellationToken.None);

        Assert.Equal(DayPeriod.Dawn, world.CurrentDayPeriod);
    }

    [Fact]
    public async Task TriggerTickAsync_UpdatesPeriodEvenWithNoPlayers()
    {
        var world = new World(Substitute.For<IWorldStore>(), NullLogger<World>.Instance);
        world.CurrentDayPeriod = DayPeriod.Morning;

        var presenter = Substitute.For<IGameMessagePresenter>();
        var timeProvider = new FakeTimeProvider();

        var clock = CreateWorldClock(world, presenter, timeProvider, dayPeriodMinutes: 10);

        timeProvider.Advance(TimeSpan.FromMinutes(10));
        await clock.TriggerTickAsync(CancellationToken.None);

        // Period should still advance even with no players
        Assert.Equal(DayPeriod.Afternoon, world.CurrentDayPeriod);
        // But no messages should be sent
        presenter.DidNotReceive().Present(Arg.Any<GameMessage>());
    }

    private static WorldClock CreateWorldClock(
        World world,
        IGameMessagePresenter presenter,
        TimeProvider timeProvider,
        int dayPeriodMinutes = 10) => new(
        world,
        presenter,
        Options.Create(new WorldClockOptions
        {
            TickIntervalSeconds = 60,
            DayPeriodDurationMinutes = dayPeriodMinutes
        }),
        timeProvider,
        NullLogger<WorldClock>.Instance);

    private static World CreateWorldWithPlayers(out List<IPlayerConnection> connections)
    {
        var world = new World(Substitute.For<IWorldStore>(), NullLogger<World>.Instance);
        connections = [];

        for (var i = 0; i < 2; i++)
        {
            var connection = Substitute.For<IPlayerConnection>();
            connection.Id.Returns($"conn-{i}");
            connection.SendMessageAsync(Arg.Any<string>()).Returns(Task.CompletedTask);

            var player = new Player
            {
                Username = $"Player {i}",
                Connection = connection
            };

            world.Players[connection.Id] = player;
            connections.Add(connection);
        }

        return world;
    }
}
