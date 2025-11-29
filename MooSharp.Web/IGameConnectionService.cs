using Microsoft.AspNetCore.SignalR.Client;
using MooSharp;

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

    // These match MooHub.cs exactly
    Task Login(string username, string password);
    Task Register(string username, string password);
    Task SendCommandAsync(string command);
    Task<AutocompleteOptions> GetAutocompleteOptions();
}