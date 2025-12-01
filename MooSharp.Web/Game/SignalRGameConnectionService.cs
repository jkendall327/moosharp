using Microsoft.AspNetCore.SignalR.Client;
using MooSharp.Game;

namespace MooSharp.Web.Game;

public sealed class SignalRGameConnectionService : IGameConnectionService
{
    private HubConnection? _hubConnection;

    public event Action<string>? OnMessageReceived;
    public event Action<bool, string>? OnLoginResult;

    public event Action? OnReconnecting;
    public event Action? OnReconnected;
    public event Action? OnClosed;

    public HubConnectionState State => _hubConnection?.State ?? HubConnectionState.Disconnected;

    public async Task InitializeAsync(Uri hubUrl, Func<Task<string?>> accessTokenProvider)
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = accessTokenProvider;
            })
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<string>("ReceiveMessage", msg => OnMessageReceived?.Invoke(msg));
        _hubConnection.On<bool, string>("LoginResult", (success, msg) => OnLoginResult?.Invoke(success, msg));

        _hubConnection.Reconnecting += _ => { OnReconnecting?.Invoke(); return Task.CompletedTask; };
        _hubConnection.Reconnected += _ => { OnReconnected?.Invoke(); return Task.CompletedTask; };
        _hubConnection.Closed += _ => { OnClosed?.Invoke(); return Task.CompletedTask; };
    }

    public async Task StartAsync()
    {
        if (_hubConnection is null)
        {
            throw new InvalidOperationException("Hub not initialized");
        }

        if (State == HubConnectionState.Disconnected)
        {
            await _hubConnection.StartAsync();
        }
    }

    public async Task StopAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.StopAsync();
        }
    }

    public async Task Login(string username, string password)
    {
        if (_hubConnection is null)
        {
            throw new InvalidOperationException("Hub not initialized.");
        }

        await _hubConnection.SendAsync("Login", username, password);
    }

    public async Task Register(string username, string password)
    {
        if (_hubConnection is null)
        {
            throw new InvalidOperationException("Hub not initialized.");
        }

        await _hubConnection.SendAsync("Register", username, password);
    }

    public async Task SendCommandAsync(string command)
    {
        if (_hubConnection is null)
        {
            throw new InvalidOperationException("Hub not initialized.");
        }

        await _hubConnection.SendAsync("SendCommand", command);
    }

    public async Task<AutocompleteOptions> GetAutocompleteOptions()
    {
        if (_hubConnection is null)
        {
            throw new InvalidOperationException("Hub connection not initialized");
        }

        return await _hubConnection.InvokeAsync<AutocompleteOptions>("GetAutocompleteOptions");
    }

    public bool IsConnected()
    {
        return State == HubConnectionState.Connected;
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}