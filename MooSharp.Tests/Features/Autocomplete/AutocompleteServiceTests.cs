using MooSharp.Features.Autocomplete;

namespace MooSharp.Tests.Features.Autocomplete;

public class AutocompleteServiceTests
{
    [Fact]
    public void GetMatch_WithSingleMatchingObject_ReturnsFullMatch()
    {
        var options = new AutocompleteOptions([], [], ["Test Gadget"]);
        var service = CreateService();

        var result = service.GetMatch(options, "x gadg");

        Assert.Equal("x Test Gadget", result);
    }

    [Fact]
    public void GetMatch_WithNoFragment_ReturnsNull()
    {
        var options = new AutocompleteOptions([], [], ["Test Gadget"]);
        var service = CreateService();

        var result = service.GetMatch(options, "x ");

        Assert.Null(result);
    }

    [Fact]
    public void GetMatch_WithEmptyCommand_ReturnsNull()
    {
        var options = new AutocompleteOptions([], [], ["Test Gadget"]);
        var service = CreateService();

        var result = service.GetMatch(options, "");

        Assert.Null(result);
    }

    [Fact]
    public void GetMatch_WithNoMatches_ReturnsNull()
    {
        var options = new AutocompleteOptions([], [], ["Test Gadget"]);
        var service = CreateService();

        var result = service.GetMatch(options, "x door");

        Assert.Null(result);
    }

    [Fact]
    public void GetMatch_WithNoCandidates_ReturnsNull()
    {
        var options = new AutocompleteOptions([], [], []);
        var service = CreateService();

        var result = service.GetMatch(options, "x gadg");

        Assert.Null(result);
    }

    [Fact]
    public void GetMatch_WithFragmentOnly_ReturnsMatchWithoutPrefix()
    {
        var options = new AutocompleteOptions([], [], ["Test Gadget"]);
        var service = CreateService();

        var result = service.GetMatch(options, "gadg");

        Assert.Equal("Test Gadget", result);
    }

    [Fact]
    public void GetMatch_IsCaseInsensitive()
    {
        var options = new AutocompleteOptions([], [], ["Test Gadget"]);
        var service = CreateService();

        var result = service.GetMatch(options, "x GADG");

        Assert.Equal("x Test Gadget", result);
    }

    [Fact]
    public void GetMatch_WithMultipleMatches_ReturnsCommonPrefix()
    {
        var options = new AutocompleteOptions([], [], ["Red Ball", "Red Box", "Red Balloon"]);
        var service = CreateService();

        var result = service.GetMatch(options, "get b");

        Assert.Equal("get Red B", result);
    }

    [Fact]
    public void GetMatch_WithMultipleMatchesAndLongerCommonPrefix_ReturnsLongerPrefix()
    {
        var options = new AutocompleteOptions([], [], ["Magic Sword", "Magic Shield"]);
        var service = CreateService();

        var result = service.GetMatch(options, "take mag");

        Assert.Equal("take Magic S", result);
    }

    [Fact]
    public void GetMatch_MatchesFromExits()
    {
        var options = new AutocompleteOptions(["north", "northeast"], [], []);
        var service = CreateService();

        var result = service.GetMatch(options, "go nor");

        Assert.Equal("go north", result);
    }

    [Fact]
    public void GetMatch_MatchesFromInventory()
    {
        var options = new AutocompleteOptions([], ["golden key"], []);
        var service = CreateService();

        var result = service.GetMatch(options, "drop gold");

        Assert.Equal("drop golden key", result);
    }

    [Fact]
    public void GetMatch_MatchesFromAllSources()
    {
        var options = new AutocompleteOptions(
            ["north"],
            ["sword"],
            ["shield"]
        );
        var service = CreateService();

        var resultExit = service.GetMatch(options, "go n");
        var resultInventory = service.GetMatch(options, "drop sw");
        var resultRoom = service.GetMatch(options, "take sh");

        Assert.Equal("go north", resultExit);
        Assert.Equal("drop sword", resultInventory);
        Assert.Equal("take shield", resultRoom);
    }

    [Fact]
    public void GetMatch_WithDuplicatesAcrossSources_TreatsThemAsOne()
    {
        var options = new AutocompleteOptions(
            [],
            ["sword"],
            ["sword"]
        );
        var service = CreateService();

        var result = service.GetMatch(options, "x sw");

        Assert.Equal("x sword", result);
    }

    [Fact]
    public void GetMatch_WithPartialWordMatch_ReturnsMatch()
    {
        var options = new AutocompleteOptions([], [], ["ancient artifact"]);
        var service = CreateService();

        var result = service.GetMatch(options, "x tifac");

        Assert.Equal("x ancient artifact", result);
    }

    [Fact]
    public void GetMatch_WithCommandWithMultipleSpaces_UsesLastFragment()
    {
        var options = new AutocompleteOptions([], [], ["Test Gadget"]);
        var service = CreateService();

        var result = service.GetMatch(options, "complex command with gadg");

        Assert.Equal("complex command with Test Gadget", result);
    }

    private static AutocompleteService CreateService()
    {
        return new AutocompleteService(null!);
    }
}
