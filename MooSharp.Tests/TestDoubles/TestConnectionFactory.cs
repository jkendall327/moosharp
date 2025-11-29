using System.Collections.Concurrent;
using MooSharp.Infrastructure;
using MooSharp.Messaging;

namespace MooSharp.Tests.TestDoubles;

public class TestConnectionFactory : IPlayerConnectionFactory
{
    // Store connections so tests can inspect them
    public ConcurrentDictionary<string, TestPlayerConnection> CreatedConnections { get; } = new();

    public IPlayerConnection Create(ConnectionId connectionId)
    {
        var conn = new TestPlayerConnection();
        CreatedConnections[connectionId.Value] = conn;
        return conn;
    }
}
