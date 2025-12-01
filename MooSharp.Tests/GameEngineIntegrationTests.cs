using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;

namespace MooSharp.Tests;

public class GameEngineIntegrationTests
{
    [Fact]
    public async Task PlayerCanRegisterAndTraverseWorld()
    {
        await using var app = new MooSharpTestApp();

        // Get the channel to send inputs into the engine
        var inputWriter = app.Services.GetRequiredService<ChannelWriter<GameInput>>();
        var connectionId = new ConnectionId("test-conn-1");

        var register = new RegisterCommand
        {
            Username = "Hero",
            Password = "password123"
        };

        await SendAndWaitAsync(inputWriter, connectionId, register);

        var move = new WorldCommand
        {
            Command = "move side-room"
        };

        await SendAndWaitAsync(inputWriter, connectionId, move);

        var world = app.Services.GetRequiredService<World>();
        var player = world.Players.Values.Single(p => p.Username == "Hero");

        var room = world.GetPlayerLocation(player)?.Id.Value;

        Assert.Equal("side-room", room);

        var conn = player.Connection as TestPlayerConnection ??
                   throw new InvalidOperationException("Test player didn't have a test connection - setup is wrong.");

        // TODO: can probably do some tests based on these.
    }

    [Fact]
    public async Task PlayerReceivesMotdAfterLogin()
    {
        const string motd = "Server maintenance tonight.";

        await using var app = new MooSharpTestApp
        {
            Motd = motd
        };

        var inputWriter = app.Services.GetRequiredService<ChannelWriter<GameInput>>();

        var registerConnection = new ConnectionId("motd-register");
        var register = new RegisterCommand
        {
            Username = "MotdHero",
            Password = "password123"
        };

        await SendAndWaitAsync(inputWriter, registerConnection, register);

        var loginConnection = new ConnectionId("motd-login");
        var login = new LoginCommand
        {
            Username = "MotdHero",
            Password = "password123"
        };

        await SendAndWaitAsync(inputWriter, loginConnection, login);

        var connection = app.ConnectionFactory.CreatedConnections[loginConnection.Value];

        Assert.True(connection.Messages.Count >= 3);
        Assert.Equal(motd, connection.Messages[1]);
    }

    private static async Task SendAndWaitAsync(ChannelWriter<GameInput> writer,
        ConnectionId connectionId,
        InputCommand command)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var input = new GameInput(connectionId, command)
        {
            CompletionSource = tcs
        };

        await writer.WriteAsync(input);

        // This line will pause the test until the GameEngine has actually finished 
        // the ProcessInput method for this specific command.
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
    }
}