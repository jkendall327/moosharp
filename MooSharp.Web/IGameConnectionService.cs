namespace MooSharp.Web;

using Microsoft.AspNetCore.SignalR.Client;

public interface IGameConnectionService : IAsyncDisposable
{
    event Action<string>? OnMessageReceived;
    event Action<bool, string>? OnLoginResult;
    event Action? OnReconnecting;
    event Action? OnReconnected;
    event Action? OnClosed;

    HubConnectionState State { get; }
    Task InitializeAsync(Uri hubUrl, Func<Task<string?>> accessTokenProvider);
    Task StartAsync();
    Task StopAsync();
    Task SendCommandAsync(string command);
    Task SendLoginAsync(string username, string password);
    Task SendRegisterAsync(string username, string password);
    Task<AutocompleteOptions> GetAutocompleteOptionsAsync();
}

// Implementation details omitted for brevity, but it wraps HubConnection
// and forwards events. See the full ViewModel integration below.