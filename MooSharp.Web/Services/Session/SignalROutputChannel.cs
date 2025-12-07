using Microsoft.AspNetCore.SignalR;
using MooSharp.Infrastructure.Sessions;
using MooSharp.Web.Services.SignalR;

namespace MooSharp.Web.Services.Session;

public class SignalROutputChannel(ISingleClientProxy client) : IOutputChannel
{
    public async Task WriteOutputAsync(string message, CancellationToken ct = default)
    {
        await client.SendAsync(MooHub.ReceiveMessage, message, ct);
    }
}