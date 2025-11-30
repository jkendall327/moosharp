using Microsoft.Extensions.Logging.Abstractions;
using MooSharp.Messaging;
using MooSharp.Web;
using NSubstitute;

namespace MooSharp.Tests;

public class GameClientViewModelTests
{
    private readonly IGameConnectionService _connection = Substitute.For<IGameConnectionService>();
    private readonly IGameHistoryService _history = Substitute.For<IGameHistoryService>();

    private readonly GameClientViewModel _viewModel;

    public GameClientViewModelTests()
    {
        _viewModel = new(_connection, _history, NullLogger<GameClientViewModel>.Instance);
    }

    [Fact]
    public async Task SubmitCommandAsync_WhenNotLoggedIn_DoesNothing()
    {
        _viewModel.CommandInput = "look";

        var stateChanges = 0;
        _viewModel.OnStateChanged += () => stateChanges++;

        await _viewModel.SubmitCommandAsync();

        await _connection
            .DidNotReceive()
            .SendCommandAsync(Arg.Any<string>());

        _history
            .DidNotReceiveWithAnyArgs()
            .AddCommand(default!);

        Assert.Equal("look", _viewModel.CommandInput);
        Assert.Equal(0, stateChanges);
    }

    [Fact]
    public async Task SubmitCommandAsync_WhenLoggedIn_SendsCommandAndClearsInput()
    {
        _connection
            .SendCommandAsync(Arg.Any<string>())
            .Returns(Task.CompletedTask);

        _history
            .PersistAsync()
            .Returns(Task.CompletedTask);

        _viewModel.CommandInput = "look";
        _connection.OnLoginResult += Raise.Event<Action<bool, string>>(true, "ok");

        var stateChanges = 0;
        _viewModel.OnStateChanged += () => stateChanges++;

        var focusRequests = 0;

        _viewModel.OnFocusInputRequested += () =>
        {
            focusRequests++;

            return Task.CompletedTask;
        };

        await _viewModel.SubmitCommandAsync();

        await _connection
            .Received(1)
            .SendCommandAsync("look");

        _history
            .Received(1)
            .AddCommand("look");

        await _history
            .Received(1)
            .PersistAsync();

        Assert.Equal(string.Empty, _viewModel.CommandInput);
        Assert.Equal(1, stateChanges);
        Assert.Equal(1, focusRequests);
    }

    [Fact]
    public async Task SubmitCommandAsync_WhenClearCommand_ClearsOutputLocally()
    {
        _connection.OnLoginResult += Raise.Event<Action<bool, string>>(true, "ok");
        _connection.OnMessageReceived += Raise.Event<Action<string>>("hello");

        var stateChanges = 0;
        _viewModel.OnStateChanged += () => stateChanges++;

        var focusRequests = 0;

        _viewModel.OnFocusInputRequested += () =>
        {
            focusRequests++;

            return Task.CompletedTask;
        };

        _viewModel.CommandInput = "/clear";

        await _viewModel.SubmitCommandAsync();

        await _connection
            .DidNotReceive()
            .SendCommandAsync(Arg.Any<string>());

        _history
            .DidNotReceiveWithAnyArgs()
            .AddCommand(default!);

        Assert.Equal(string.Empty, _viewModel.GameOutput);
        Assert.Equal(string.Empty, _viewModel.CommandInput);
        Assert.Equal(1, stateChanges);
        Assert.Equal(1, focusRequests);
    }

    [Fact]
    public void NavigateHistory_RestoresDraftAfterCycling()
    {
        _history.CommandHistory.Returns(new List<string>
        {
            "first",
            "second"
        });

        _viewModel.CommandInput = "draft";

        var stateChanges = 0;
        _viewModel.OnStateChanged += () => stateChanges++;

        _viewModel.NavigateHistory(-1);
        Assert.Equal("second", _viewModel.CommandInput);

        _viewModel.NavigateHistory(1);
        Assert.Equal("draft", _viewModel.CommandInput);
        Assert.Equal(2, stateChanges);
    }

    [Fact]
    public async Task PerformAutocompleteAsync_CompletesInputAndRequestsFocus()
    {
        _connection
            .GetAutocompleteOptions()
            .Returns(new AutocompleteOptions([
                    "north", "south"
                ],
                []));

        _viewModel.CommandInput = "go n";
        _connection.OnLoginResult += Raise.Event<Action<bool, string>>(true, "ok");

        var stateChanges = 0;
        _viewModel.OnStateChanged += () => stateChanges++;

        var focusRequests = 0;

        _viewModel.OnFocusInputRequested += () =>
        {
            focusRequests++;

            return Task.CompletedTask;
        };

        await _viewModel.PerformAutocompleteAsync();

        Assert.Equal("go north", _viewModel.CommandInput);
        Assert.Equal(1, stateChanges);
        Assert.Equal(1, focusRequests);

        await _connection
            .Received(1)
            .GetAutocompleteOptions();
    }

    [Fact]
    public async Task ToggleChannelAsync_WhenLoggedInAndConnected_SendsMuteCommand()
    {
        _connection
            .IsConnected()
            .Returns(true);

        _connection
            .SendCommandAsync(Arg.Any<string>())
            .Returns(Task.CompletedTask);

        _connection.OnLoginResult += Raise.Event<Action<bool, string>>(true, "ok");

        var stateChanges = 0;
        _viewModel.OnStateChanged += () => stateChanges++;

        await _viewModel.ToggleChannelAsync(ChatChannels.Global, true);

        Assert.True(_viewModel.ChannelMuteState[ChatChannels.Global]);

        await _connection
            .Received(1)
            .SendCommandAsync("mute Global");

        Assert.Equal(1, stateChanges);
    }
}