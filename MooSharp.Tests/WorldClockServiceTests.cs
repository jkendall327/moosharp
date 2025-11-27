using NSubstitute;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MooSharp.Messaging;
using MooSharp.Persistence;

namespace MooSharp.Tests;

public class WorldClockServiceTests
{
    [Fact]
    public async Task TriggerTickAsync_SendsMessagesToAllPlayers()
    {
        var world = CreateWorldWithPlayers(out var connections);
        var presenter = Substitute.For<IGameMessagePresenter>();
        presenter.Present(Arg.Any<GameMessage>()).Returns("event");

        var clock = CreateWorldClock(world, presenter);

        await clock.TriggerTickAsync(CancellationToken.None);

        foreach (var connection in connections)
        {
            await connection.Received(1).SendMessageAsync(Arg.Any<string>());
        }

        presenter.Received(world.Players.Count).Present(Arg.Any<GameMessage>());
    }

    [Fact]
    public async Task TriggerTickAsync_DoesNothingWhenNoPlayers()
    {
        var world = new World(Substitute.For<IWorldStore>(), NullLogger<World>.Instance);
        var presenter = Substitute.For<IGameMessagePresenter>();

        var clock = CreateWorldClock(world, presenter);

        await clock.TriggerTickAsync(CancellationToken.None);

        presenter.DidNotReceive().Present(Arg.Any<GameMessage>());
    }

    private static WorldClock CreateWorldClock(World world, IGameMessagePresenter presenter) => new(
        world,
        presenter,
        Options.Create(new WorldClockOptions
        {
            TickIntervalSeconds = 1,
            Events = ["event"]
        }),
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
