using MooSharp.Commands.Commands.Informational;
using Object = MooSharp.Actors.Object;

namespace MooSharp.Tests.Handlers;

public class InventoryHandlerTests
{
    [Fact]
    public async Task InventoryHandler_ReturnsInventoryExaminedEventWithInventory()
    {
        var player = HandlerTestHelpers.CreatePlayer();

        var item = new Object
        {
            Name = "Lantern",
            Description = "An old lantern"
        };

        item.MoveTo(player);

        var handler = new InventoryHandler();

        var result = await handler.Handle(new()
        {
            Player = player
        });

        var message = Assert.Single(result.Messages);
        var evt = Assert.IsType<InventoryExaminedEvent>(message.Event);
        Assert.Contains(item, evt.Inventory);
    }
}