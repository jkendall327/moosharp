using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;

namespace MooSharp.Web;

public class TelnetServer(
    IServiceProvider serviceProvider,
    PlayerGameLoopManager manager,
    PlayerMultiplexer multiplexer,
    //LoginManager loginManager,
    IOptions<AppOptions> options,
    ILogger<TelnetServer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // using var listener = new TcpListener(IPAddress.Any, options.Value.ServerPort);
        //
        // listener.Start();
        //
        // while (!stoppingToken.IsCancellationRequested)
        // {
        //     var client = await listener.AcceptTcpClientAsync(stoppingToken);
        //
        //     logger.LogInformation("TCP client connected");
        //
        //     // For each new connection, create a new scope to resolve services
        //     // This ensures each player connection has its own dependencies if needed
        //     _ = Task.Run(async () =>
        //         {
        //             await using var scope = serviceProvider.CreateAsyncScope();
        //
        //             // Resolve the shared world and a new player connection handler
        //             var conn = await loginManager.AttemptLoginAsync(client.GetStream(), stoppingToken);
        //
        //             if (conn is null)
        //             {
        //                 return;
        //             }
        //             
        //             logger.BeginScope(new Dictionary<string, object?>()
        //             {
        //                 {
        //                     "ConnectionId", conn.Id
        //                 }
        //             });
        //
        //             logger.LogInformation("Player connected");
        //
        //             multiplexer.AddPlayer(conn);
        //
        //             await manager.RunMainLoopAsync(conn, stoppingToken);
        //
        //             logger.LogInformation("Player disconnected");
        //         },
        //         stoppingToken);
        // }
        //
        // logger.LogInformation("TCP server stopped");
    }
}