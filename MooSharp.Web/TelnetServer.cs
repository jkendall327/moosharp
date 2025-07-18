using System.Net;
using System.Net.Sockets;

namespace MooSharp.Web;

public class TelnetServer(IServiceProvider serviceProvider) : BackgroundService
{
    private readonly List<PlayerConnection> _connections = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new TcpListener(IPAddress.Any, 8888);

        listener.Start();

        while (!stoppingToken.IsCancellationRequested)
        {
            var client = await listener.AcceptTcpClientAsync(stoppingToken);

            // For each new connection, create a new scope to resolve services
            // This ensures each player connection has its own dependencies if needed
            _ = Task.Run(async () =>
                {
                    await using var scope = serviceProvider.CreateAsyncScope();

                    // Resolve the shared world and a new player connection handler
                    var conn = new PlayerConnection(client);

                    _connections.Add(conn);

                    await conn.ProcessCommandsAsync();
                },
                stoppingToken);
        }
    }
}