using Microsoft.AspNetCore.SignalR.Client;
using MooSharp.Messaging;

namespace MooSharp.Web;

using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

public class GameViewModel : IDisposable
{
    private readonly IGameConnectionService _connection;
    private readonly IClientStorageService _storage;
    private readonly ILogger<GameViewModel> _logger;
    private readonly NavigationManager _navManager;

    private const string SessionStorageKey = "mooSharpSession";
    private const string CommandHistoryStorageKey = "mooSharpCommandHistory";
    private const int CommandHistoryLimit = 20;

    // State
    private readonly StringBuilder _outputBuffer = new();
    private readonly List<string> _commandHistory = new();
    private int _historyIndex = -1;
    private string _commandDraft = string.Empty;

    // Bindable Properties
    public string GameOutput => _outputBuffer.ToString();
    public string CommandInput { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string LoginStatus { get; private set; } = string.Empty;
    public bool IsLoggedIn { get; private set; }
    public bool IsConnected => _connection.State == HubConnectionState.Connected;

    public bool CanSubmitCredentials =>
        IsConnected && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);

    public Dictionary<string, bool> ChannelMuteState { get; } = new(StringComparer.OrdinalIgnoreCase);
    public string[] AvailableChannels { get; } = [ChatChannels.Global, ChatChannels.Newbie, ChatChannels.Trade];

    // Events to notify the View
    public event Action? OnStateChanged;
    public event Func<Task>? OnFocusInputRequested;

    public GameViewModel(IGameConnectionService connection,
        IClientStorageService storage,
        ILogger<GameViewModel> logger,
        NavigationManager navManager)
    {
        _connection = connection;
        _storage = storage;
        _logger = logger;
        _navManager = navManager;

        // Subscribe to connection events
        _connection.OnMessageReceived += msg =>
        {
            _outputBuffer.AppendLine(msg);
            NotifyStateChanged();
        };

        _connection.OnLoginResult += HandleLoginResult;

        _connection.OnReconnecting += () =>
        {
            IsLoggedIn = false;
            NotifyStateChanged();
        };

        _connection.OnReconnected += () =>
        {
            IsLoggedIn = false;
            NotifyStateChanged();
        }; // Reset auth on reconnect

        _connection.OnClosed += () =>
        {
            IsLoggedIn = false;
            NotifyStateChanged();
        };

        InitializeChannels();
    }

    public async Task InitializeAsync()
    {
        await LoadCommandHistoryAsync();

        var sessionId = await GetOrCreateSessionIdAsync();
        var hubUri = _navManager.ToAbsoluteUri("/moohub");

        await _connection.InitializeAsync(hubUri, () => Task.FromResult<string?>(sessionId));

        try
        {
            await _connection.StartAsync();
        }
        catch (Exception ex)
        {
            _outputBuffer.AppendLine($"Starting hub failed: {ex.Message}");
        }

        NotifyStateChanged();
    }

    public async Task SubmitCommandAsync()
    {
        if (string.IsNullOrWhiteSpace(CommandInput) || !IsLoggedIn)
        {
            return;
        }

        try
        {
            await _connection.SendCommandAsync(CommandInput);

            AddCommandToHistory(CommandInput);
            await SaveCommandHistoryAsync();

            CommandInput = string.Empty;
            _historyIndex = -1;
            _commandDraft = string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending command");
        }

        NotifyStateChanged();

        if (OnFocusInputRequested is not null)
        {
            await OnFocusInputRequested.Invoke();
        }
    }

    public void NavigateHistory(int delta)
    {
        if (_commandHistory.Count == 0)
        {
            return;
        }

        if (_historyIndex == -1)
        {
            _commandDraft = CommandInput;
            _historyIndex = _commandHistory.Count;
        }

        var nextIndex = Math.Clamp(_historyIndex + delta, 0, _commandHistory.Count);

        if (nextIndex == _commandHistory.Count)
        {
            _historyIndex = -1;
            CommandInput = _commandDraft;
        }
        else
        {
            _historyIndex = nextIndex;
            CommandInput = _commandHistory[_historyIndex];
        }

        NotifyStateChanged();
    }

    public async Task PerformAutocompleteAsync()
    {
        if (!IsLoggedIn)
        {
            return;
        }

        var options = await _connection.GetAutocompleteOptionsAsync();

        var candidates = options
            .Exits
            .Concat(options.InventoryItems)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!candidates.Any())
        {
            return;
        }

        var (prefix, fragment) = SplitCommandInput(CommandInput);

        if (string.IsNullOrWhiteSpace(fragment))
        {
            return;
        }

        var matches = candidates
            .Where(c => c.StartsWith(fragment, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!matches.Any())
        {
            return;
        }

        var completion = matches.Count == 1 ? matches[0] : FindCommonPrefix(matches, fragment);
        CommandInput = $"{prefix}{completion}";

        NotifyStateChanged();

        if (OnFocusInputRequested is not null)
        {
            await OnFocusInputRequested.Invoke();
        }
    }

    public async Task LoginAsync()
    {
        LoginStatus = "Logging in...";
        NotifyStateChanged();

        try
        {
            await _connection.SendLoginAsync(Username, Password);
        }
        catch (Exception)
        {
            LoginStatus = "Failed to send login request.";
            NotifyStateChanged();
        }
    }

    public async Task RegisterAsync()
    {
        LoginStatus = "Registering...";
        NotifyStateChanged();

        try
        {
            await _connection.SendRegisterAsync(Username, Password);
        }
        catch (Exception)
        {
            LoginStatus = "Failed to send register request.";
            NotifyStateChanged();
        }
    }

    public async Task LogoutAsync()
    {
        LoginStatus = "Logging out...";
        await _storage.RemoveItemAsync(SessionStorageKey);
        await _connection.StopAsync();

        IsLoggedIn = false;
        CommandInput = string.Empty;
        Username = string.Empty;
        Password = string.Empty;
        InitializeChannels(); // Reset mutes

        var msg = "Logged out. Session cleared.";
        _outputBuffer.AppendLine(msg);
        LoginStatus = msg;

        // Re-init connection to generate new session ID
        await InitializeAsync();
    }

    public async Task ToggleChannelAsync(string channel, bool isMuted)
    {
        ChannelMuteState[channel] = isMuted;

        if (IsLoggedIn)
        {
            var cmd = isMuted ? $"mute {channel}" : $"unmute {channel}";
            await _connection.SendCommandAsync(cmd);
        }

        NotifyStateChanged();
    }

    private void HandleLoginResult(bool success, string message)
    {
        IsLoggedIn = success;
        LoginStatus = message;
        _outputBuffer.AppendLine(message);
        NotifyStateChanged();
    }

    private async Task<string> GetOrCreateSessionIdAsync()
    {
        var existing = await _storage.GetItemAsync(SessionStorageKey);

        if (!string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        var newId = Guid
            .NewGuid()
            .ToString();

        await _storage.SetItemAsync(SessionStorageKey, newId);

        return newId;
    }

    private void InitializeChannels()
    {
        ChannelMuteState.Clear();

        foreach (var c in AvailableChannels)
        {
            ChannelMuteState[c] = false;
        }
    }

    // Pure logic helper methods (Excellent for unit testing)
    public static (string Prefix, string Fragment) SplitCommandInput(string input)
    {
        var lastSpace = input.LastIndexOf(' ');

        if (lastSpace == -1)
        {
            return (string.Empty, input);
        }

        return (input[..(lastSpace + 1)], input[(lastSpace + 1)..]);
    }

    public static string FindCommonPrefix(List<string> options, string seed)
    {
        // ... (Same logic as before) ...
        // Re-implementing briefly for completeness
        if (options.Count == 0)
        {
            return seed;
        }

        var prefix = seed;
        var reference = options[0];

        for (var i = seed.Length; i < reference.Length; i++)
        {
            var candidate = reference[..(i + 1)];

            if (options.Any(o => !o.StartsWith(candidate, StringComparison.OrdinalIgnoreCase)))
            {
                break;
            }

            prefix = candidate;
        }

        return prefix;
    }

    private async Task LoadCommandHistoryAsync()
    {
        var json = await _storage.GetItemAsync(CommandHistoryStorageKey);

        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        try
        {
            var history = JsonSerializer.Deserialize<List<string>>(json);

            if (history != null)
            {
                _commandHistory.Clear();
                _commandHistory.AddRange(history.TakeLast(CommandHistoryLimit));
            }
        }
        catch
        {
            /* Ignore corruption */
        }
    }

    private async Task SaveCommandHistoryAsync()
    {
        var json = JsonSerializer.Serialize(_commandHistory
            .TakeLast(CommandHistoryLimit)
            .ToList());

        await _storage.SetItemAsync(CommandHistoryStorageKey, json);
    }

    private void AddCommandToHistory(string cmd)
    {
        var trimmed = cmd.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return;
        }

        _commandHistory.Remove(trimmed);
        _commandHistory.Add(trimmed);

        if (_commandHistory.Count > CommandHistoryLimit)
        {
            _commandHistory.RemoveAt(0);
        }
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();

    public void Dispose()
    {
        // Unsubscribe events if necessary, though simpler in DI scopes
    }
}