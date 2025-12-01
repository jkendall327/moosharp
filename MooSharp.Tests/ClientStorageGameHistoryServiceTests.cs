using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using MooSharp.Web;
using NSubstitute;

namespace MooSharp.Tests;

public class ClientStorageGameHistoryServiceTests
{
    [Fact]
    public async Task InitializeAsync_LoadsHistoryFromStorage()
    {
        var storedHistory = new List<string> { "first", "second", "first" };
        var storage = Substitute.For<IClientStorageService>();
        storage.GetItemAsync("mooSharpCommandHistory")
            .Returns(JsonSerializer.Serialize(storedHistory));

        var service = CreateService(storage);

        await service.InitializeAsync();

        Assert.Equal(["second", "first"], service.CommandHistory);
    }

    [Fact]
    public void AddCommand_DeduplicatesAndLimitsHistory()
    {
        var storage = Substitute.For<IClientStorageService>();
        var service = CreateService(storage);

        for (var i = 0; i < 15; i++)
        {
            service.AddCommand($"cmd{i}");
        }

        service.AddCommand("cmd5");
        service.AddCommand(" cmd6 ");

        for (var i = 15; i < 25; i++)
        {
            service.AddCommand($"cmd{i}");
        }

        Assert.Equal(20, service.CommandHistory.Count);
        Assert.DoesNotContain("cmd0", service.CommandHistory);
        Assert.Contains("cmd5", service.CommandHistory);
        Assert.DoesNotContain("cmd1", service.CommandHistory.Take(2));
        Assert.Equal("cmd24", service.CommandHistory[^1]);
    }

    [Fact]
    public async Task PersistAsync_WritesSerializedHistory()
    {
        var storage = Substitute.For<IClientStorageService>();
        var service = CreateService(storage);
        var commands = new[] { "one", "two" };

        foreach (var command in commands)
        {
            service.AddCommand(command);
        }

        await service.PersistAsync();

        await storage.Received(1).SetItemAsync("mooSharpCommandHistory", Arg.Any<string>());

        var savedJson = storage.ReceivedCalls()
            .Single(call => call.GetMethodInfo().Name == nameof(IClientStorageService.SetItemAsync))
            .GetArguments()[1] as string;

        var deserialized = JsonSerializer.Deserialize<List<string>>(savedJson ?? string.Empty);

        Assert.NotNull(deserialized);
        Assert.Equal(commands, deserialized);
    }

    [Fact]
    public async Task GetOrCreateSessionIdAsync_UsesStoredValue()
    {
        var storage = Substitute.For<IClientStorageService>();
        storage.GetItemAsync("mooSharpSession").Returns("known");

        var service = CreateService(storage);

        var result = await service.GetOrCreateSessionIdAsync();

        Assert.Equal("known", result);
        await storage.DidNotReceive().SetItemAsync("mooSharpSession", Arg.Any<string>());
    }

    [Fact]
    public async Task GetOrCreateSessionIdAsync_CreatesAndCachesWhenMissing()
    {
        var storage = Substitute.For<IClientStorageService>();
        storage.GetItemAsync("mooSharpSession").Returns((string?)null);

        var service = CreateService(storage);

        var result = await service.GetOrCreateSessionIdAsync();

        Assert.False(string.IsNullOrWhiteSpace(result));
        await storage.Received(1).SetItemAsync("mooSharpSession", result);
    }

    private static ClientStorageGameHistoryService CreateService(IClientStorageService storage)
    {
        return new(storage, NullLogger<ClientStorageGameHistoryService>.Instance);
    }
}
