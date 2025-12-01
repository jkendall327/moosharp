using MooSharp.Messaging;

namespace MooSharp.Tests.TestDoubles;

public sealed class TestPlayerConnection : IPlayerConnection
{
    public string Id { get; } = Guid.NewGuid().ToString();

    public List<string> Messages { get; } = [];

    public Task SendMessageAsync(string message, CancellationToken ct = default)
    {
        Messages.Add(message);
        return Task.CompletedTask;
    }
}