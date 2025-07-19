using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Options;

namespace MooSharp;

public class PlayerConnection
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    private readonly TcpClient _client;
    private readonly CommandParser _parser;
    private readonly IOptions<AppOptions> _options;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;

    public Player PlayerObject { get; private set; }

    private static readonly Dictionary<string, string> _logins = new()
    {
        {
            "test", "123"
        }
    };

    public PlayerConnection(TcpClient client, CommandParser parser, IOptions<AppOptions> options)
    {
        _client = client;
        _parser = parser;
        _options = options;

        var stream = _client.GetStream();

        _reader = new(stream);

        _writer = new(stream)
        {
            AutoFlush = true
        };
    }

    public async Task SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        await _writer.WriteLineAsync(message);
    }
    
    public async Task SendMessageAsync(StringBuilder message, CancellationToken cancellationToken = default)
    {
        await _writer.WriteLineAsync(message, cancellationToken);
    }

    public async Task ProcessCommandsAsync(CancellationToken token = default)
    {
        await SendMessageAsync("Welcome to the C# MOO!", token);

        if (_options.Value.RequireLogins)
        {
            if (await AttemptLoginAsync(token))
            {
                return;
            }
        }

        // This is the main loop for a single player
        while (_client.Connected)
        {
            try
            {
                var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));

                var linked = CancellationTokenSource.CreateLinkedTokenSource(token, timeout.Token);

                var command = await _reader.ReadLineAsync(linked.Token);

                if (command == null)
                {
                    // Client disconnected
                    break;
                }

                PlayerObject = new()
                {
                    Username = "foo"
                };

                // Pass the raw text command to the command parser
                await _parser.ParseAsync(PlayerObject, command, token);
            }
            catch (IOException)
            {
                break;
            }
        }
    }

    private async Task<bool> AttemptLoginAsync(CancellationToken token = default)
    {
        await SendMessageAsync("Please enter your username.", token);
        
        var username = await _reader.ReadLineAsync(token);

        await SendMessageAsync("Please enter your password.", token);

        var password = await _reader.ReadLineAsync(token);

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            await SendMessageAsync("Login failed.", token);

            return true;
        }

        if (!_logins.TryGetValue(username, out var pass) || !string.Equals(password, pass, StringComparison.Ordinal))
        {
            await SendMessageAsync("Login failed.", token);

            return true;
        }

        await SendMessageAsync("You are now logged in.", token);

        return false;
    }
}