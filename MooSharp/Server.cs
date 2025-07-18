using System.Net;
using System.Net.Sockets;

namespace MooSharp;

public class Server
{
    private TcpListener _listener;

    public async Task StartAsync(int port)
    {
        _listener = new(IPAddress.Any, port);
        _listener.Start();
        Console.WriteLine($"MOO server started on port {port}...");

        while (true)
        {
            // Asynchronously wait for a new client to connect.
            // This doesn't block the server.
            var client = await _listener.AcceptTcpClientAsync();
            
            // When a client connects, hand them off to a new task to be handled.
            // The server immediately goes back to waiting for the next connection.
            _ = Task.Run(() => HandleClientAsync(client)); 
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        Console.WriteLine("New client connected!");
        
        var connection = new PlayerConnection(client);
        await connection.ProcessCommandsAsync(); 

        Console.WriteLine("Client disconnected.");
    }
}