using Microsoft.AspNetCore.SignalR;
using MooSharp.Infrastructure;

namespace MooSharp.Web.Services;

public class SignalROutputChannel(ISingleClientProxy client) : IOutputChannel
{
    public async Task WriteOutputAsync(string message, CancellationToken ct = default)
    {
        await client.SendAsync(MooHub.ReceiveMessage, message, ct);
    }
}