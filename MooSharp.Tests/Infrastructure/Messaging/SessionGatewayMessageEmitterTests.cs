using Microsoft.Extensions.Logging;
using MooSharp.Actors.Players;
using MooSharp.Commands.Presentation;
using MooSharp.Features.Editor;
using MooSharp.Infrastructure.Messaging;
using MooSharp.Infrastructure.Sessions;
using NSubstitute;

namespace MooSharp.Tests.Infrastructure.Messaging;

public class SessionGatewayMessageEmitterTests
{
    [Fact]
    public async Task SendGameMessagesAsync_DispatchesMessages_WhenPlayerNotInEditorMode()
    {
        var player = new Player { Id = PlayerId.New(), Username = "Tester" };
        var gameMessage = new GameMessage(player, new TestEvent());

        var gateway = Substitute.For<ISessionGateway>();
        var presenter = Substitute.For<IGameMessagePresenter>();
        var editorModeService = Substitute.For<IEditorModeService>();
        var logger = Substitute.For<ILogger<SessionGatewayMessageEmitter>>();

        presenter.Present(gameMessage).Returns("hello world");
        editorModeService.IsInEditorMode(player.Id.Value).Returns(false);

        var emitter = new SessionGatewayMessageEmitter(gateway, presenter, editorModeService, logger);

        await emitter.SendGameMessagesAsync([gameMessage]);

        await gateway.Received(1)
            .DispatchToActorAsync(player.Id.Value, "hello world", Arg.Any<CancellationToken>());
        presenter.Received(1).Present(gameMessage);
    }

    [Fact]
    public async Task SendGameMessagesAsync_SkipsMessages_WhenPlayerInEditorMode()
    {
        var player = new Player { Id = PlayerId.New(), Username = "Tester" };
        var gameMessage = new GameMessage(player, new TestEvent());

        var gateway = Substitute.For<ISessionGateway>();
        var presenter = Substitute.For<IGameMessagePresenter>();
        var editorModeService = Substitute.For<IEditorModeService>();
        var logger = Substitute.For<ILogger<SessionGatewayMessageEmitter>>();

        editorModeService.IsInEditorMode(player.Id.Value).Returns(true);

        var emitter = new SessionGatewayMessageEmitter(gateway, presenter, editorModeService, logger);

        await emitter.SendGameMessagesAsync([gameMessage]);

        await gateway.DidNotReceiveWithAnyArgs()
            .DispatchToActorAsync(default, default!, Arg.Any<CancellationToken>());
        presenter.DidNotReceiveWithAnyArgs().Present(Arg.Any<GameMessage>());
    }

    private sealed class TestEvent : IGameEvent;
}
