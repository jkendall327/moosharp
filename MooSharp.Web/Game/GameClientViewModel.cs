using System.Text;
using MooSharp;
using MooSharp.Messaging;
using MooSharp.Web;

public sealed class GameClientViewModel : IDisposable
{
    // Dependencies
    private readonly IGameConnectionService _connection;
    private readonly IGameHistoryService _historyService;
    private readonly ILogger<GameClientViewModel> _logger;

    // Internal State
    private readonly StringBuilder _outputBuffer = new();
    private int _historyIndex = -1;
    private string _commandDraft = string.Empty;

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
    public bool IsConnected => _connection.IsConnected();

    public bool CanSubmitCredentials =>
        IsConnected && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);

    // Channel preferences
    public Dictionary<string, bool> ChannelMuteState { get; } = new(StringComparer.OrdinalIgnoreCase);
    public string[] AvailableChannels { get; } = [ChatChannels.Global, ChatChannels.Newbie, ChatChannels.Trade];

    // --- Events ---
    public event Action? OnStateChanged;
    public event Func<Task>? OnFocusInputRequested;

    private Uri? _hubUri;
    
    public GameClientViewModel(IGameConnectionService connection,
        IGameHistoryService historyService,
        ILogger<GameClientViewModel> logger)
    {
        _connection = connection;
        _historyService = historyService;
        _logger = logger;

        _connection.OnMessageReceived += HandleMessageReceived;
        _connection.OnLoginResult += HandleLoginResult;
        _connection.OnReconnecting += HandleReconnecting;
        _connection.OnReconnected += HandleReconnected;
        _connection.OnClosed += HandleClosed;

        InitializeChannels();
    }

    public async Task InitializeAsync(Uri hub)
    {
        await _historyService.InitializeAsync();
        var sessionId = await _historyService.GetOrCreateSessionIdAsync();
        
        // Pass the session ID provider to the connection service
        _hubUri = hub;
        await _connection.InitializeAsync(hub, () => Task.FromResult<string?>(sessionId));

        try
        {
            await _connection.StartAsync();
        }
        catch (Exception ex)
        {
            _outputBuffer.AppendLine($"Starting hub failed: {ex.Message}");
            _logger.LogError(ex, "Failed to start hub connection");
        }

        NotifyStateChanged();
    }

    public async Task SubmitCommandAsync()
    {
        if (string.IsNullOrWhiteSpace(CommandInput) || !IsLoggedIn)
        {
            return;
        }

        var commandToSend = CommandInput;

        try
        {
            await _connection.SendCommandAsync(commandToSend);

            _historyService.AddCommand(commandToSend);
            await _historyService.PersistAsync();

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
        if (!CanSubmitCredentials)
        {
            return;
        }

        LoginStatus = "Logging in...";
        NotifyStateChanged();

        try
        {
            await _connection.Login(Username, Password);
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
        if (!CanSubmitCredentials)
        {
            return;
        }

        LoginStatus = "Registering...";
        NotifyStateChanged();

        try
        {
            await _connection.Register(Username, Password);
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
        await _historyService.ClearSessionAsync();

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

        if (_hubUri is null)
        {
            throw new InvalidOperationException("Hub URI was null when trying to log out.");
        }
        
        await InitializeAsync(_hubUri);
    }

    public void NavigateHistory(int delta)
    {
        var history = _historyService.CommandHistory;

        if (history.Count == 0)
        {
            return;
        }

        // If we are currently editing a new line, save it as draft
        if (_historyIndex == -1)
        {
            _commandDraft = CommandInput;
            _historyIndex = history.Count;
        }

        var nextIndex = _historyIndex + delta;

        // Clamp index
        if (nextIndex < 0)
        {
            nextIndex = 0;
        }

        if (nextIndex > history.Count)
        {
            nextIndex = history.Count;
        }

        if (nextIndex == history.Count)
        {
            // Restored the draft
            _historyIndex = -1;
            CommandInput = _commandDraft;
        }
        else
        {
            // Show history item
            _historyIndex = nextIndex;
            CommandInput = history[_historyIndex];
        }

        NotifyStateChanged();
    }

    public async Task PerformAutocompleteAsync()
    {
        if (!IsLoggedIn)
        {
            return;
        }

        AutocompleteOptions options;

        try
        {
            options = await _connection.GetAutocompleteOptions();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch autocomplete options");

            return;
        }

        var candidates = options
            .Exits
            .Concat(options.InventoryItems)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (candidates.Count == 0)
        {
            return;
        }

        (var prefix, var fragment) = SplitCommandInput(CommandInput);

        if (string.IsNullOrWhiteSpace(fragment))
        {
            return;
        }

        var matches = candidates
            .Where(c => c.StartsWith(fragment, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            return;
        }

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
        if (!ChannelMuteState.ContainsKey(channel))
        {
            return;
        }

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
        _logger.LogWarning("Reconnecting...");
        IsLoggedIn = false;
        NotifyStateChanged();
    }

    private void HandleReconnected()
    {
        _logger.LogInformation("Reconnected");
        IsLoggedIn = false;
        NotifyStateChanged();
    }

    private void HandleClosed()
    {
        _logger.LogWarning("Connection closed");
        IsLoggedIn = false;
        NotifyStateChanged();
    }

    private void InitializeChannels()
    {
        foreach (var channel in AvailableChannels)
        {
            ChannelMuteState[channel] = false;
        }
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();

    private static (string Prefix, string Fragment) SplitCommandInput(string input)
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

    private static string FindCommonPrefix(List<string> options, string seed)
    {
        if (options.Count == 0)
        {
            return seed;
        }

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
