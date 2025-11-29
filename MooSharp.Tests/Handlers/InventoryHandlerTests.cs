namespace MooSharp.Tests;

public class InventoryHandlerTests
{
    [Fact]
    public async Task InventoryHandler_ReturnsSelfExaminedEventWithInventory()
    {
        var player = HandlerTestHelpers.CreatePlayer();

        var item = new Object
        {
            Name = "Lantern",
            Description = "An old lantern"
        };

        item.MoveTo(player);

        var handler = new InventoryHandler();

        var result = await handler.Handle(new InventoryCommand
        {
            Player = player
        });

        var message = Assert.Single(result.Messages);
        var evt = Assert.IsType<SelfExaminedEvent>(message.Event);
        Assert.Contains(item, evt.Inventory);
    }
}