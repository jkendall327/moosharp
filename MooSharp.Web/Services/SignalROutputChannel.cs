using Microsoft.AspNetCore.SignalR;
using MooSharp.Infrastructure;

namespace MooSharp.Web.Services;

public class SignalROutputChannel(ISingleClientProxy client) : IOutputChannel
{
    public async Task WriteOutputAsync(string message, CancellationToken ct = default)
    {
        throw new NotImplementedException("Use constants for the method names.");
        await client.SendAsync("output", message, ct);
    }
}