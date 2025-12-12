using MooSharp.Commands.Commands.Creative;
using MooSharp.Commands.Presentation;
using MooSharp.Data;
using MooSharp.Data.Players;
using NSubstitute;

namespace MooSharp.Tests.Handlers;

public class DescribeSelfHandlerTests
{
    [Fact]
    public async Task DescribeSelfHandler_UpdatesDescriptionAndPersists()
    {
        var repo = Substitute.For<IPlayerRepository>();
        var player = HandlerTestHelpers.CreatePlayer("Player");
        var handler = new DescribeSelfHandler(repo);

        var result = await handler.Handle(new DescribeSelfCommand
        {
            Player = player,
            NewDescription = "A daring explorer."
        });

        Assert.Equal("A daring explorer.", player.Description);

        await repo
            .Received(1)
            .UpdatePlayerDescriptionAsync(player.Id.Value, "A daring explorer.", WriteType.Immediate, Arg.Any<CancellationToken>());

        var message = Assert.Single(result.Messages);
        var evt = Assert.IsType<SystemMessageEvent>(message.Event);
        Assert.Contains("A daring explorer.", evt.Message);
    }
}
