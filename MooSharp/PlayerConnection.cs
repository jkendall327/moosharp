using System.Net.Sockets;

namespace MooSharp;

public class PlayerConnection
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    private readonly TcpClient _client;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;

    //public Player PlayerObject { get; private set; } // The actual in-game player object

    private static readonly Dictionary<string, string> _logins = new()
    {
        {
            "test", "123"
        }
    };

    public PlayerConnection(TcpClient client)
    {
        _client = client;
        var stream = _client.GetStream();
        _reader = new StreamReader(stream);

        _writer = new StreamWriter(stream)
        {
            AutoFlush = true
        };
    }

    public async Task SendMessageAsync(string message)
    {
        await _writer.WriteLineAsync(message);
    }

    public async Task ProcessCommandsAsync()
    {
        // ... Handle login/character creation here ...

        await SendMessageAsync("Welcome to the C# MOO!");
        await SendMessageAsync("Please enter your username.");

        var username = await _reader.ReadLineAsync();

        await SendMessageAsync("Please enter your password.");

        var password = await _reader.ReadLineAsync();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            await SendMessageAsync("Login failed.");
            return;
        }
        
        if (!_logins.TryGetValue(username, out var pass) || !string.Equals(password, pass, StringComparison.Ordinal))
        {
            await SendMessageAsync("Login failed.");
            return;
        }

        await SendMessageAsync("You are now logged in.");
        
        // This is the main loop for a single player
        while (_client.Connected)
        {
            try
            {
                var command = await _reader.ReadLineAsync();

                if (command == null) break; // Client disconnected

                // Pass the raw text command to the command parser
                //CommandParser.ParseAndExecute(PlayerObject, command);
            }
            catch (IOException)
            {
                break; // Connection lost
            }
        }
    }
}