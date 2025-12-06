using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using MooSharp.Actors;
using MooSharp.Data;
using MooSharp.Data.Mapping;
using MooSharp.Infrastructure;
using MooSharp.Messaging;
using MooSharp.World;
using NSubstitute;

namespace MooSharp.Tests;

// TODO: recreate these
public class WorldClockServiceTests
{
    // [Fact]
    // public async Task TriggerTickAsync_DoesNotBroadcastBeforePeriodDurationElapsed()
    // {
    //     var world = CreateWorldWithPlayers(out var _);
    //     var presenter = Substitute.For<IGameMessagePresenter>();
    //     var timeProvider = new FakeTimeProvider();
    //
    //     var clock = CreateWorldClock(world, presenter, timeProvider, dayPeriodMinutes: 10);
    //
    //     // Advance only 5 minutes - not enough
    //     timeProvider.Advance(TimeSpan.FromMinutes(5));
    //     await clock.TriggerTickAsync(CancellationToken.None);
    //
    //     presenter.DidNotReceive().Present(Arg.Any<GameMessage>());
    // }
    //
    // [Fact]
    // public async Task TriggerTickAsync_BroadcastsWhenPeriodDurationElapsed()
    // {
    //     var world = CreateWorldWithPlayers(out var connections);
    //     var presenter = Substitute.For<IGameMessagePresenter>();
    //     presenter.Present(Arg.Any<GameMessage>()).Returns("event");
    //     var timeProvider = new FakeTimeProvider();
    //
    //     var clock = CreateWorldClock(world, presenter, timeProvider, dayPeriodMinutes: 10);
    //
    //     // Advance 10 minutes - triggers period change
    //     timeProvider.Advance(TimeSpan.FromMinutes(10));
    //     await clock.TriggerTickAsync(CancellationToken.None);
    //
    //     foreach (var connection in connections)
    //     {
    //         await connection.Received(1).SendMessageAsync(Arg.Any<string>());
    //     }
    // }
    //
    // [Fact]
    // public async Task TriggerTickAsync_AdvancesDayPeriod()
    // {
    //     var world = CreateWorldWithPlayers(out var _);
    //     world.CurrentDayPeriod = DayPeriod.Morning;
    //
    //     var presenter = Substitute.For<IGameMessagePresenter>();
    //     presenter.Present(Arg.Any<GameMessage>()).Returns("event");
    //     var timeProvider = new FakeTimeProvider();
    //
    //     var clock = CreateWorldClock(world, presenter, timeProvider, dayPeriodMinutes: 10);
    //
    //     timeProvider.Advance(TimeSpan.FromMinutes(10));
    //     await clock.TriggerTickAsync(CancellationToken.None);
    //
    //     Assert.Equal(DayPeriod.Afternoon, world.CurrentDayPeriod);
    // }
    //
    // [Fact]
    // public async Task TriggerTickAsync_CyclesThroughAllPeriods()
    // {
    //     var world = CreateWorldWithPlayers(out var _);
    //     world.CurrentDayPeriod = DayPeriod.Night;
    //
    //     var presenter = Substitute.For<IGameMessagePresenter>();
    //     presenter.Present(Arg.Any<GameMessage>()).Returns("event");
    //     var timeProvider = new FakeTimeProvider();
    //
    //     var clock = CreateWorldClock(world, presenter, timeProvider, dayPeriodMinutes: 10);
    //
    //     // Night should cycle back to Dawn
    //     timeProvider.Advance(TimeSpan.FromMinutes(10));
    //     await clock.TriggerTickAsync(CancellationToken.None);
    //
    //     Assert.Equal(DayPeriod.Dawn, world.CurrentDayPeriod);
    // }

    // [Fact]
    // public async Task TriggerTickAsync_UpdatesPeriodEvenWithNoPlayers()
    // {
    //     var world = new World.World(Substitute.For<IWorldRepository>(), NullLogger<World.World>.Instance)
    //     {
    //         CurrentDayPeriod = DayPeriod.Morning
    //     };
    //
    //     var presenter = Substitute.For<IGameMessageEmitter>();
    //     var timeProvider = new FakeTimeProvider();
    //
    //     var clock = CreateWorldClock(world, presenter, timeProvider, dayPeriodMinutes: 10);
    //
    //     timeProvider.Advance(TimeSpan.FromMinutes(10));
    //     await clock.TriggerTickAsync(CancellationToken.None);
    //
    //     // Period should still advance even with no players
    //     Assert.Equal(DayPeriod.Afternoon, world.CurrentDayPeriod);
    //
    //     // But no messages should be sent
    //
    //     throw new NotImplementedException();
    //
    //     //presenter.DidNotReceive().SendGameMessagesAsync(Arg.Any<GameMessage>());
    // }

    private static WorldClock CreateWorldClock(World.World world,
        IGameMessageEmitter emitter,
        TimeProvider timeProvider,
        int dayPeriodMinutes = 10)
    {
        return new(world,
            Options.Create(new WorldClockOptions
            {
                TickIntervalSeconds = 60,
                DayPeriodDurationMinutes = dayPeriodMinutes
            }),
            timeProvider,
            emitter,
            NullLogger<WorldClock>.Instance);
    }

    private static World.World CreateWorldWithPlayers(out List<IPlayerConnection> connections)
    {
        var world = new World.World(Substitute.For<IWorldRepository>(), NullLogger<World.World>.Instance);
        connections = [];

        for (var i = 0; i < 2; i++)
        {
            var connection = Substitute.For<IPlayerConnection>();
            connection.Id.Returns($"conn-{i}");

            connection
                .SendMessageAsync(Arg.Any<string>())
                .Returns(Task.CompletedTask);

            var player = new Player
            {
                Id = PlayerId.New(),
                Username = $"Player {i}"
            };

            world.Players[connection.Id] = player;
            connections.Add(connection);
        }

        return world;
    }
}