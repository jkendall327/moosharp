using System.Text.Json;
using MooSharp.Game;

namespace MooSharp.Web.Services.ClientStorage;

public sealed class ClientStorageGameHistoryService(
    IClientStorageService storage,
    ILogger<ClientStorageGameHistoryService> logger) : IGameHistoryService
{
    private const string CommandHistoryStorageKey = "mooSharpCommandHistory";
    private const int CommandHistoryLimit = 20;

    private readonly List<string> _commandHistory = [];

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

            await storage.SetItemAsync(CommandHistoryStorageKey, historyJson);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save command history");
        }
    }

    private async Task LoadCommandHistoryAsync()
    {
        try
        {
            var historyJson = await storage.GetItemAsync(CommandHistoryStorageKey);

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
            logger.LogWarning(ex, "Failed to load command history");
        }
    }
}
