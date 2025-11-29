namespace MooSharp.Web.Game;

using Microsoft.AspNetCore.SignalR.Client;

public class GameConnectionService : IGameConnectionService
{
    private HubConnection? _hubConnection;

    // Events defined in the interface
    public event Action<string>? OnMessageReceived;
    public event Action<bool, string>? OnLoginResult;
    public event Action? OnReconnecting;
    public event Action? OnReconnected;
    public event Action? OnClosed;

    public HubConnectionState State => _hubConnection?.State ?? HubConnectionState.Disconnected;

    public async Task InitializeAsync(Uri hubUrl, Func<Task<string?>> accessTokenProvider)
    {
        // If we are re-initializing, dispose the old one
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => { options.AccessTokenProvider = accessTokenProvider; })
            .WithAutomaticReconnect()
            .Build();

        // Wire up Server-to-Client listeners
        _hubConnection.On<string>(MooHub.ReceiveMessage, (message) => { OnMessageReceived?.Invoke(message); });

        _hubConnection.On<bool, string>(MooHub.LoginResult,
            (success, message) => { OnLoginResult?.Invoke(success, message); });

        // Wire up Lifecycle events
        _hubConnection.Reconnecting += (error) =>
        {
            OnReconnecting?.Invoke();

            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += (connectionId) =>
        {
            OnReconnected?.Invoke();

            return Task.CompletedTask;
        };

        _hubConnection.Closed += (error) =>
        {
            OnClosed?.Invoke();

            return Task.CompletedTask;
        };
    }

    public async Task StartAsync()
    {
        if (_hubConnection == null)
            throw new InvalidOperationException("Connection not initialized.");

        if (State == HubConnectionState.Disconnected)
        {
            await _hubConnection.StartAsync();
        }
    }

    public async Task StopAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
        }
    }

    public async Task SendCommandAsync(string command)
    {
        if (_hubConnection == null) return;

        await _hubConnection.SendAsync(MooHub.SendCommand, command);
    }

    public async Task SendLoginAsync(string username, string password)
    {
        if (_hubConnection == null) return;

        await _hubConnection.SendAsync(MooHub.Login, username, password);
    }

    public async Task SendRegisterAsync(string username, string password)
    {
        if (_hubConnection == null) return;

        await _hubConnection.SendAsync(MooHub.Register, username, password);
    }

    public async Task<AutocompleteOptions> GetAutocompleteOptionsAsync()
    {
        if (_hubConnection == null) return new AutocompleteOptions();

        return await _hubConnection.InvokeAsync<AutocompleteOptions>(MooHub.GetAutocompleteOptions);
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }
}