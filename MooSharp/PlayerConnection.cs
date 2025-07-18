using System.Net.Sockets;

namespace MooSharp;

public class PlayerConnection
{
    private readonly TcpClient _client;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;
    //public Player PlayerObject { get; private set; } // The actual in-game player object

    public PlayerConnection(TcpClient client)
    {
        _client = client;
        var stream = _client.GetStream();
        _reader = new StreamReader(stream);
        _writer = new StreamWriter(stream) { AutoFlush = true };
    }

    public async Task SendMessageAsync(string message)
    {
        await _writer.WriteLineAsync(message);
    }

    public async Task ProcessCommandsAsync()
    {
        // ... Handle login/character creation here ...
        
        await SendMessageAsync("Welcome to the C# MOO!");
        
        // This is the main loop for a single player
        while (_client.Connected)
        {
            try
            {
                string command = await _reader.ReadLineAsync();
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