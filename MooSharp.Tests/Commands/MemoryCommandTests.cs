using MooSharp.Actors.Players;
using MooSharp.Commands.Commands.Memory;
using MooSharp.Commands.Machinery;

namespace MooSharp.Tests.Commands;

public class MemoryCommandTests
{
    [Fact]
    public async Task Remember_AddsMemory()
    {
        // Arrange
        var player = new Player
        {
            Id = PlayerId.New(),
            Username = "Tester",
            Memories = []
        };
        var handler = new RememberHandler();
        var command = new RememberCommand { Player = player, Text = "Buy milk" };

        // Act
        var result = await handler.Handle(command);

        // Assert
        Assert.Single(player.Memories);
        Assert.Equal("Buy milk", player.Memories[0]);
        Assert.IsType<PlayerRememberedEvent>(result.Events[player].First());
    }

    [Fact]
    public async Task Recall_ReturnsMemories()
    {
        // Arrange
        var player = new Player
        {
            Id = PlayerId.New(),
            Username = "Tester",
            Memories = ["Buy milk", "Walk dog"]
        };
        var handler = new RecallHandler();
        var command = new RecallCommand { Player = player };

        // Act
        var result = await handler.Handle(command);

        // Assert
        var evt = Assert.IsType<MemoriesRecalledEvent>(result.Events[player].First());
        Assert.Equal(2, evt.Memories.Count);
        Assert.Equal("Buy milk", evt.Memories[0]);
        Assert.Equal("Walk dog", evt.Memories[1]);
    }

    [Fact]
    public async Task Recall_SpecificIndex_ReturnsMemory()
    {
        // Arrange
        var player = new Player
        {
            Id = PlayerId.New(),
            Username = "Tester",
            Memories = ["Buy milk", "Walk dog"]
        };
        var handler = new RecallHandler();
        var command = new RecallCommand { Player = player, Index = 2 };

        // Act
        var result = await handler.Handle(command);

        // Assert
        var evt = Assert.IsType<SingleMemoryRecalledEvent>(result.Events[player].First());
        Assert.Equal("Walk dog", evt.Memory);
        Assert.Equal(2, evt.Index);
    }

    [Fact]
    public async Task Forget_RemovesMemory()
    {
        // Arrange
        var player = new Player
        {
            Id = PlayerId.New(),
            Username = "Tester",
            Memories = ["Buy milk", "Walk dog"]
        };
        var handler = new ForgetHandler();
        var command = new ForgetCommand { Player = player, Index = 1 };

        // Act
        var result = await handler.Handle(command);

        // Assert
        Assert.Single(player.Memories);
        Assert.Equal("Walk dog", player.Memories[0]);
        Assert.IsType<MemoryForgottenEvent>(result.Events[player].First());
    }
}
