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
            Description = "A sturdy wooden door"
        };

        obj.IsOpenable = true;
        obj.IsLockable = true;
        obj.IsLocked = true;

        var description = obj.DescribeWithState();

        Assert.Equal("A sturdy wooden door (It is closed and locked.)", description);
    }
}
