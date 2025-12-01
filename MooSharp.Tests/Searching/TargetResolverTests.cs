using MooSharp.Commands.Searching;
using Object = MooSharp.Actors.Object;

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

        var result = resolver.FindObjects(Array.Empty<Object>(), query);

        Assert.True(result.IsSelf);
        Assert.Equal(SearchStatus.Found, result.Status);
    }
}
