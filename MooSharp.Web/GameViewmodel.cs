using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MooSharp;
using MooSharp.Messaging;
using MooSharp.Web;

public class GameViewModel : IDisposable
{
    // Dependencies
    private readonly IGameConnectionService _connection;
    private readonly IClientStorageService _storage;
    private readonly ILogger<GameViewModel> _logger;
    private readonly NavigationManager _navManager;

    // Constants
    private const string SessionStorageKey = "mooSharpSession";
    private const string CommandHistoryStorageKey = "mooSharpCommandHistory";
    private const int CommandHistoryLimit = 20;

    // Internal State
    private readonly StringBuilder _outputBuffer = new();
    private readonly List<string> _commandHistory = new();
    private int _historyIndex = -1;
    private string _commandDraft = string.Empty;

    // --- Public Properties (Bound to View) ---

    // The giant string of text for the terminal output
    public string GameOutput => _outputBuffer.ToString();

    // The text currently in the input box
    public string CommandInput { get; set; } = string.Empty;

    // Auth fields
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    // Status fields
    public string LoginStatus { get; private set; } = string.Empty;
    public bool IsLoggedIn { get; private set; }

    // Derived properties for UI state
    public bool IsConnected => _connection.State == HubConnectionState.Connected;

    public bool CanSubmitCredentials =>
        IsConnected && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);

    // Channel preferences
    public Dictionary<string, bool> ChannelMuteState { get; } = new(StringComparer.OrdinalIgnoreCase);
    public string[] AvailableChannels { get; } = [ChatChannels.Global, ChatChannels.Newbie, ChatChannels.Trade];

    // --- Events ---
    public event Action? OnStateChanged; // Tells Blazor to re-render
    public event Func<Task>? OnFocusInputRequested; // Tells Blazor to focus the <input>

    // Constructor
    public GameViewModel(IGameConnectionService connection,
        IClientStorageService storage,
        ILogger<GameViewModel> logger,
        NavigationManager navManager)
    {
        _connection = connection;
        _storage = storage;
        _logger = logger;
        _navManager = navManager;

        // Subscribe to Service Events
        _connection.OnMessageReceived += HandleMessageReceived;
        _connection.OnLoginResult += HandleLoginResult;
        _connection.OnReconnecting += HandleReconnecting;
        _connection.OnReconnected += HandleReconnected;
        _connection.OnClosed += HandleClosed;

        InitializeChannels();
    }

    // --- Initialization ---

    public async Task InitializeAsync()
    {
        await LoadCommandHistoryAsync();
        var sessionId = await GetOrCreateSessionIdAsync();

        var hubUri = _navManager.ToAbsoluteUri("/moohub");

        // Pass the session ID provider to the connection service
        await _connection.InitializeAsync(hubUri, () => Task.FromResult<string?>(sessionId));

        try
        {
            await _connection.StartAsync();
        }
        catch (Exception ex)
        {
            _outputBuffer.AppendLine($"Starting hub failed: {ex.Message}");
            _logger.LogError(ex, "Failed to start hub connection.");
        }

        NotifyStateChanged();
    }

    // --- User Actions ---

    public async Task SubmitCommandAsync()
    {
        if (string.IsNullOrWhiteSpace(CommandInput) || !IsLoggedIn)
            return;

        var commandToSend = CommandInput; // Capture current input

        try
        {
            await _connection.SendCommandAsync(commandToSend);

            AddCommandToHistory(commandToSend);
            await SaveCommandHistoryAsync();

            // Clear input after successful send
            CommandInput = string.Empty;
            _historyIndex = -1;
            _commandDraft = string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending command");
            _outputBuffer.AppendLine($"Error sending command: {ex.Message}");
        }

        NotifyStateChanged();

        if (OnFocusInputRequested is not null)
        {
            await OnFocusInputRequested.Invoke();
        }
    }

    public async Task LoginAsync()
    {
        if (!CanSubmitCredentials) return;

        LoginStatus = "Logging in...";
        NotifyStateChanged();

        try
        {
            await _connection.SendLoginAsync(Username, Password);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed");
            LoginStatus = "Failed to send login request.";
            NotifyStateChanged();
        }
    }

    public async Task RegisterAsync()
    {
        if (!CanSubmitCredentials) return;

        LoginStatus = "Registering...";
        NotifyStateChanged();

        try
        {
            await _connection.SendRegisterAsync(Username, Password);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed");
            LoginStatus = "Failed to send register request.";
            NotifyStateChanged();
        }
    }

    public async Task LogoutAsync()
    {
        LoginStatus = "Logging out...";
        NotifyStateChanged();

        // 1. Clear Local Storage
        await _storage.RemoveItemAsync(SessionStorageKey);

        // 2. Stop Connection
        try
        {
            await _connection.StopAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping connection during logout");
        }

        // 3. Reset State
        IsLoggedIn = false;
        CommandInput = string.Empty;
        Username = string.Empty;
        Password = string.Empty;
        InitializeChannels();

        const string logoutMessage = "Logged out. Session cleared.";
        _outputBuffer.AppendLine(logoutMessage);
        LoginStatus = logoutMessage;

        // 4. Re-Initialize (generates new Session ID)
        await InitializeAsync();
    }

    public void NavigateHistory(int delta)
    {
        if (_commandHistory.Count == 0) return;

        // If we are currently editing a new line, save it as draft
        if (_historyIndex == -1)
        {
            _commandDraft = CommandInput;
            _historyIndex = _commandHistory.Count;
        }

        var nextIndex = _historyIndex + delta;

        // Clamp index
        if (nextIndex < 0) nextIndex = 0;
        if (nextIndex > _commandHistory.Count) nextIndex = _commandHistory.Count;

        if (nextIndex == _commandHistory.Count)
        {
            // Restored the draft
            _historyIndex = -1;
            CommandInput = _commandDraft;
        }
        else
        {
            // Show history item
            _historyIndex = nextIndex;
            CommandInput = _commandHistory[_historyIndex];
        }

        NotifyStateChanged();
    }

    public async Task PerformAutocompleteAsync()
    {
        if (!IsLoggedIn) return;

        AutocompleteOptions options;

        try
        {
            options = await _connection.GetAutocompleteOptionsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch autocomplete options.");

            return;
        }

        var candidates = options
            .Exits
            .Concat(options.InventoryItems)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (candidates.Count == 0) return;

        var (prefix, fragment) = SplitCommandInput(CommandInput);

        if (string.IsNullOrWhiteSpace(fragment)) return;

        var matches = candidates
            .Where(c => c.StartsWith(fragment, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0) return;

        var completion = matches.Count == 1 ? matches[0] : FindCommonPrefix(matches, fragment);

        // Append the completion to the prefix
        CommandInput = $"{prefix}{completion}";

        NotifyStateChanged();
        
        if (OnFocusInputRequested is not null)
        {
            await OnFocusInputRequested.Invoke();
        }
    }

    public async Task ToggleChannelAsync(string channel, bool isMuted)
    {
        if (!ChannelMuteState.ContainsKey(channel)) return;

        ChannelMuteState[channel] = isMuted;

        if (IsLoggedIn && IsConnected)
        {
            var command = isMuted ? $"mute {channel}" : $"unmute {channel}";

            try
            {
                await _connection.SendCommandAsync(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to toggle channel mute state");
            }
        }

        NotifyStateChanged();
    }

    // --- Private Event Handlers ---

    private void HandleMessageReceived(string message)
    {
        _outputBuffer.AppendLine(message);
        NotifyStateChanged();
    }

    private void HandleLoginResult(bool success, string message)
    {
        IsLoggedIn = success;
        LoginStatus = message;
        _outputBuffer.AppendLine(message);
        NotifyStateChanged();
    }

    private void HandleReconnecting()
    {
        _logger.LogWarning("SignalR reconnecting...");
        IsLoggedIn = false;
        NotifyStateChanged();
    }

    private void HandleReconnected()
    {
        _logger.LogInformation("SignalR reconnected.");
        IsLoggedIn = false;
        NotifyStateChanged();
    }

    private void HandleClosed()
    {
        _logger.LogWarning("SignalR connection closed.");
        IsLoggedIn = false;
        NotifyStateChanged();
    }

    // --- Helpers ---

    private async Task<string> GetOrCreateSessionIdAsync()
    {
        var existingSessionId = await _storage.GetItemAsync(SessionStorageKey);

        if (!string.IsNullOrWhiteSpace(existingSessionId))
        {
            return existingSessionId;
        }

        var newSessionId = Guid
            .NewGuid()
            .ToString();

        await _storage.SetItemAsync(SessionStorageKey, newSessionId);

        return newSessionId;
    }

    private void InitializeChannels()
    {
        foreach (var channel in AvailableChannels)
        {
            if (ChannelMuteState.ContainsKey(channel))
                ChannelMuteState[channel] = false;
            else
                ChannelMuteState.Add(channel, false);
        }
    }

    private void AddCommandToHistory(string command)
    {
        var trimmed = command.Trim();

        if (string.IsNullOrWhiteSpace(trimmed)) return;

        _commandHistory.Remove(trimmed); // Move to bottom if exists
        _commandHistory.Add(trimmed);

        if (_commandHistory.Count > CommandHistoryLimit)
        {
            _commandHistory.RemoveAt(0);
        }
    }

    private async Task LoadCommandHistoryAsync()
    {
        try
        {
            var historyJson = await _storage.GetItemAsync(CommandHistoryStorageKey);

            if (string.IsNullOrWhiteSpace(historyJson)) return;

            var history = JsonSerializer.Deserialize<List<string>>(historyJson);

            if (history is null) return;

            _commandHistory.Clear();

            foreach (var command in history.TakeLast(CommandHistoryLimit))
            {
                AddCommandToHistory(command);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load command history.");
        }
    }

    private async Task SaveCommandHistoryAsync()
    {
        try
        {
            var historyJson = JsonSerializer.Serialize(_commandHistory
                .TakeLast(CommandHistoryLimit)
                .ToList());

            await _storage.SetItemAsync(CommandHistoryStorageKey, historyJson);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save command history.");
        }
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();

    // --- Static Logic (Pure functions) ---

    public static (string Prefix, string Fragment) SplitCommandInput(string input)
    {
        var lastSpaceIndex = input.LastIndexOf(' ');

        if (lastSpaceIndex == -1)
        {
            return (string.Empty, input);
        }

        var prefix = input[..(lastSpaceIndex + 1)];
        var fragment = input[(lastSpaceIndex + 1)..];

        return (prefix, fragment);
    }

    public static string FindCommonPrefix(List<string> options, string seed)
    {
        if (options.Count == 0) return seed;

        var comparison = StringComparison.OrdinalIgnoreCase;
        var prefix = seed;
        var reference = options[0];

        for (var i = seed.Length; i < reference.Length; i++)
        {
            var candidate = reference[..(i + 1)];

            if (options.Any(option => !option.StartsWith(candidate, comparison)))
            {
                break;
            }

            prefix = candidate;
        }

        return prefix;
    }

    public void Dispose()
    {
        _connection.OnMessageReceived -= HandleMessageReceived;
        _connection.OnLoginResult -= HandleLoginResult;
        _connection.OnReconnecting -= HandleReconnecting;
        _connection.OnReconnected -= HandleReconnected;
        _connection.OnClosed -= HandleClosed;
    }
}