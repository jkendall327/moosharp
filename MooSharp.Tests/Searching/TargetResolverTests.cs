using MooSharp.Commands.Searching;

namespace MooSharp.Tests.Searching;

public class TargetResolverTests
{
    [Theory]
    [InlineData("me")]
    [InlineData("self")]
    [InlineData("myself")]
    public void FindObjects_ReturnsSelfFlagForSelfReferences(string query)
    {
        var resolver = new TargetResolver();

        var result = resolver.FindObjects([], query);

        Assert.True(result.IsSelf);
        Assert.Equal(SearchStatus.Found, result.Status);
    }
}
