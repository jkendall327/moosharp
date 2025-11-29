using Microsoft.AspNetCore.SignalR.Client;
using MooSharp;

public class GameConnectionService : IGameConnectionService
{
    private HubConnection? _hubConnection;

    // Events that the Hub "Client" receives
    public event Action<string>? OnMessageReceived;
    public event Action<bool, string>? OnLoginResult;
    
    // Connection Lifecycle Events
    public event Action? OnReconnecting;
    public event Action? OnReconnected;
    public event Action? OnClosed;

    public HubConnectionState State => _hubConnection?.State ?? HubConnectionState.Disconnected;

    public async Task InitializeAsync(Uri hubUrl, Func<Task<string?>> accessTokenProvider)
    {
        if (_hubConnection is not null) await _hubConnection.DisposeAsync();

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                // This populates the "access_token" query string, 
                // which your MooHub.GetSessionId() logic looks for.
                options.AccessTokenProvider = accessTokenProvider;
            })
            .WithAutomaticReconnect()
            .Build();

        // --- REGISTER LISTENERS (Server -> Client) ---
        
        // These strings must match what you call in `writer.TryWrite(...)` 
        // effectively, assuming your background service broadcasts these method names.
        _hubConnection.On<string>("ReceiveMessage", msg => OnMessageReceived?.Invoke(msg));
        _hubConnection.On<bool, string>("LoginResult", (success, msg) => OnLoginResult?.Invoke(success, msg));

        // --- LIFECYCLE HOOKS ---
        _hubConnection.Reconnecting += _ => { OnReconnecting?.Invoke(); return Task.CompletedTask; };
        _hubConnection.Reconnected += _ => { OnReconnected?.Invoke(); return Task.CompletedTask; };
        _hubConnection.Closed += _ => { OnClosed?.Invoke(); return Task.CompletedTask; };
    }

    public async Task StartAsync()
    {
        if (_hubConnection is null) throw new InvalidOperationException("Hub not initialized");
        if (State == HubConnectionState.Disconnected) await _hubConnection.StartAsync();
    }

    public async Task StopAsync()
    {
        if (_hubConnection is not null) await _hubConnection.StopAsync();
    }

    // --- WRAPPERS AROUND MOOHUB METHODS (Client -> Server) ---

    public async Task Login(string username, string password)
    {
        // Calls: public Task Login(string username, string password)
        await _hubConnection.SendAsync("Login", username, password);
    }

    public async Task Register(string username, string password)
    {
        // Calls: public Task Register(string username, string password)
        await _hubConnection.SendAsync("Register", username, password);
    }

    public async Task SendCommandAsync(string command)
    {
        // Calls: public Task SendCommand(string command)
        await _hubConnection.SendAsync("SendCommand", command);
    }

    public async Task<AutocompleteOptions> GetAutocompleteOptions()
    {
        // Calls: public Task<AutocompleteOptions> GetAutocompleteOptions()
        if (_hubConnection is null) throw new InvalidOperationException("Hub connection not initialized");
        
        return await _hubConnection.InvokeAsync<AutocompleteOptions>("GetAutocompleteOptions");
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}