using MooSharp.Messaging;

namespace MooSharp.Infrastructure;

public interface IPlayerConnectionFactory
{
    IPlayerConnection Create(ConnectionId connectionId);
}