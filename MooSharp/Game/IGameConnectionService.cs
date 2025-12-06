namespace MooSharp.Game;

public interface IGameConnectionService : IAsyncDisposable
{
    event Action<string>? OnMessageReceived;
    event Action<bool, string>? OnLoginResult;
    event Action? OnReconnecting;
    event Action? OnReconnected;
    event Action? OnClosed;

    Task InitializeAsync(Uri hubUrl, Func<Task<string?>> accessTokenProvider);
    Task StartAsync();
    Task StopAsync();

    Task SendCommandAsync(string command);
    Task<AutocompleteOptions> GetAutocompleteOptions();

    bool IsConnected();
}