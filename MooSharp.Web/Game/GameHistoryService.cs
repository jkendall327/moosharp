using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace MooSharp.Web;

public interface IGameHistoryService
{
    IReadOnlyList<string> CommandHistory { get; }
    Task InitializeAsync();
    void AddCommand(string command);
    Task PersistAsync();
    Task<string> GetOrCreateSessionIdAsync();
    Task ClearSessionAsync();
}

public sealed class GameHistoryService : IGameHistoryService
{
    private const string SessionStorageKey = "mooSharpSession";
    private const string CommandHistoryStorageKey = "mooSharpCommandHistory";
    private const int CommandHistoryLimit = 20;

    private readonly IClientStorageService _storage;
    private readonly ILogger<GameHistoryService> _logger;
    private readonly List<string> _commandHistory = [];

    public GameHistoryService(IClientStorageService storage, ILogger<GameHistoryService> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public IReadOnlyList<string> CommandHistory => _commandHistory;

    public async Task InitializeAsync()
    {
        await LoadCommandHistoryAsync();
    }

    public void AddCommand(string command)
    {
        var trimmed = command.Trim();

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

    public async Task PersistAsync()
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
            _logger.LogWarning(ex, "Failed to save command history");
        }
    }

    public async Task<string> GetOrCreateSessionIdAsync()
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

    public async Task ClearSessionAsync()
    {
        await _storage.RemoveItemAsync(SessionStorageKey);
    }

    private async Task LoadCommandHistoryAsync()
    {
        try
        {
            var historyJson = await _storage.GetItemAsync(CommandHistoryStorageKey);

            if (string.IsNullOrWhiteSpace(historyJson))
            {
                return;
            }

            var history = JsonSerializer.Deserialize<List<string>>(historyJson);

            if (history is null)
            {
                return;
            }

            _commandHistory.Clear();

            foreach (var command in history.TakeLast(CommandHistoryLimit))
            {
                AddCommand(command);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load command history");
        }
    }
}
