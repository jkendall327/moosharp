using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;

namespace MooSharp.Web;

public class TelnetServer(IServiceProvider serviceProvider, IOptions<AppOptions> options, ILogger<TelnetServer> logger) : BackgroundService
{
    private readonly List<PlayerConnection> _connections = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var listener = new TcpListener(IPAddress.Any, options.Value.ServerPort);

        listener.Start();

        while (!stoppingToken.IsCancellationRequested)
        {
            var client = await listener.AcceptTcpClientAsync(stoppingToken);
            
            logger.LogInformation("TCP client connected");

            // For each new connection, create a new scope to resolve services
            // This ensures each player connection has its own dependencies if needed
            _ = Task.Run(async () =>
                {
                    await using var scope = serviceProvider.CreateAsyncScope();

                    // Resolve the shared world and a new player connection handler
                    var conn = new PlayerConnection(client, options);

                    logger.BeginScope(new Dictionary<string, object?>()
                    {
                        {
                            "ConnectionId", conn.Id
                        }
                    });
                    
                    logger.LogInformation("Player connected");
                    
                    _connections.Add(conn);

                    await conn.ProcessCommandsAsync();
                    
                    logger.LogInformation("Player disconnected");
                },
                stoppingToken);
        }
        
        logger.LogInformation("TCP server stopped");
    }
}