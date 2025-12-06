using System.Text;
using Microsoft.Extensions.Logging;
using MooSharp.Messaging;
using MooSharp.Web.Endpoints;

namespace MooSharp.Game;

public sealed class GameClientViewModel : IDisposable
{
    // Dependencies
    private readonly IHttpClientFactory _factory;
    private readonly IGameConnectionService _connection;
    private readonly IGameHistoryService _historyService;
    private readonly ILogger<GameClientViewModel> _logger;

    // Internal State
    private readonly StringBuilder _outputBuffer = new();
    private int _historyIndex = -1;
    private string _commandDraft = string.Empty;
    private Uri? _hubUri;
    private string? _jwt;

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
    public bool CanSubmitCredentials => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);

    // Channel preferences
    public Dictionary<string, bool> ChannelMuteState { get; } = new(StringComparer.OrdinalIgnoreCase);
    public string[] AvailableChannels { get; } = [ChatChannels.Global, ChatChannels.Newbie, ChatChannels.Trade];

    // Events
    public event Action? OnStateChanged;
    public event Func<Task>? OnFocusInputRequested;

    public GameClientViewModel(IHttpClientFactory factory,
        IGameConnectionService connection,
        IGameHistoryService historyService,
        ILogger<GameClientViewModel> logger)
    {
        _factory = factory;
        _connection = connection;
        _historyService = historyService;
        _logger = logger;

        _connection.OnMessageReceived += HandleMessageReceived;
        _connection.OnReconnecting += HandleReconnecting;
        _connection.OnReconnected += HandleReconnected;
        _connection.OnClosed += HandleClosed;

        InitializeChannels();
    }

    public async Task InitializeAsync(Uri hub)
    {
        await _historyService.InitializeAsync();

        _hubUri = hub;

        // TODO: try to get JWT from client storage to support refreshes again.
        await _connection.InitializeAsync(hub, () => Task.FromResult(_jwt));

        NotifyStateChanged();
    }

    private async Task StartConnection()
    {
        try
        {
            await _connection.StartAsync();
        }
        catch (Exception ex)
        {
            _outputBuffer.AppendLine($"Starting hub failed: {ex.Message}");
            _logger.LogError(ex, "Failed to start hub connection");
        }
    }

    public async Task SubmitCommandAsync()
    {
        if (string.IsNullOrWhiteSpace(CommandInput) || !IsLoggedIn)
        {
            return;
        }

        var commandToSend = CommandInput;

        if (TryHandleClientCommand(commandToSend))
        {
            await RequestFocusAsync();

            return;
        }

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

        await RequestFocusAsync();
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
            var client = _factory.CreateClient(nameof(AuthEndpoints));

            var request = new LoginRequest(Username, Password);

            var response = await client.PostAsJsonAsync(AuthEndpoints.LoginEndpoint, request);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<LoginAttemptResult>();

            if (result is null)
            {
                throw new InvalidOperationException("Deserialisation of registration result failed.");
            }

            _jwt = result.Token;

            LoginStatus = "Logged in.";
            IsLoggedIn = true;
            
            NotifyStateChanged();

            await StartConnection();
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
            var client = _factory.CreateClient(nameof(AuthEndpoints));

            var request = new RegisterRequest(Username, Password);

            var response = await client.PostAsJsonAsync(AuthEndpoints.RegistrationEndpoint, request);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<RegisterResult>();

            if (result is null)
            {
                throw new InvalidOperationException("Deserialisation of registration result failed.");
            }

            // TODO: store JWT in history so it survives refreshes again.
            _jwt = result.Token;
            LoginStatus = "Registered.";
            IsLoggedIn = true;

            NotifyStateChanged();
            
            await StartConnection();
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

        // Stop connection.
        try
        {
            await _connection.StopAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping connection during logout");
        }

        // Reset state.
        _jwt = null;
        IsLoggedIn = false;
        CommandInput = string.Empty;
        Username = string.Empty;
        Password = string.Empty;
        InitializeChannels();

        const string logoutMessage = "Logged out.";
        _outputBuffer.AppendLine(logoutMessage);
        LoginStatus = logoutMessage;

        NotifyStateChanged();
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

        if (IsLoggedIn)
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

    private bool TryHandleClientCommand(string command)
    {
        if (!string.Equals(command.Trim(), "/clear", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        _outputBuffer.Clear();
        CommandInput = string.Empty;
        _historyIndex = -1;
        _commandDraft = string.Empty;

        NotifyStateChanged();

        return true;
    }

    private async Task RequestFocusAsync()
    {
        if (OnFocusInputRequested is not null)
        {
            await OnFocusInputRequested.Invoke();
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
        _connection.OnReconnecting -= HandleReconnecting;
        _connection.OnReconnected -= HandleReconnected;
        _connection.OnClosed -= HandleClosed;
    }
}