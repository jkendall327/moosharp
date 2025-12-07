using Object = MooSharp.Actors.Objects.Object;

namespace MooSharp.Tests;

public class ObjectStateTests
{
    [Fact]
    public void DescribeWithState_ReturnsDescriptionWhenNoState()
    {
        var obj = new Object
        {
            Name = "rock",
            Description = "A smooth river rock"
        };

        Assert.Equal(obj.Description, obj.DescribeWithState());
    }

    [Fact]
    public void DescribeWithState_AppendsOpenAndLockState()
    {
        var obj = new Object
        {
            Name = "door",
            Description = "A sturdy wooden door",
            IsOpenable = true,
            IsLockable = true,
            IsLocked = true
        };

        var description = obj.DescribeWithState();

        Assert.Equal("A sturdy wooden door (It is closed and locked.)", description);
    }
}
